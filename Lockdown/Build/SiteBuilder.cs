namespace Lockdown.Build
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text;
    using AutoMapper;
    using Lockdown.Build.Entities;
    using Lockdown.Build.Markdown;
    using Lockdown.Build.Utils;
    using Raw = Lockdown.Build.RawEntities;

    public class SiteBuilder : ISiteBuilder
    {
        private readonly IFileSystem fileSystem;
        private readonly IYamlParser yamlParser;
        private readonly IMarkdownRenderer markdownRenderer;
        private readonly ILiquidRenderer liquidRenderer;
        private readonly ISlugifier slugifier;
        private readonly IMapper mapper;
        private SiteConfiguration siteConfiguration;

        public SiteBuilder(
            IFileSystem fileSystem,
            IYamlParser yamlParser,
            IMarkdownRenderer markdownRenderer,
            ILiquidRenderer liquidRenderer,
            ISlugifier slugifier,
            IMapper mapper)
        {
            this.fileSystem = fileSystem;
            this.yamlParser = yamlParser;
            this.markdownRenderer = markdownRenderer;
            this.liquidRenderer = liquidRenderer;
            this.slugifier = slugifier;
            this.mapper = mapper;
            this.siteConfiguration = null;
        }

        public virtual void CleanFolder(string folder)
        {
            if (this.fileSystem.Directory.Exists(folder))
            {
                this.fileSystem.Directory.Delete(folder, recursive: true);
            }

            this.fileSystem.Directory.CreateDirectory(folder);
        }

        public void Build(string inputPath, string outputPath)
        {
            var staticPath = this.fileSystem.Path.Combine(inputPath, "static");
            this.CleanFolder(outputPath);
            this.CopyFiles(staticPath, outputPath);
            this.liquidRenderer.SetRoot(inputPath);

            var rawSiteConfiguration = this.ReadSiteConfiguration(inputPath);
            this.siteConfiguration = this.mapper.Map<SiteConfiguration>(rawSiteConfiguration);

            var postMetadata = new List<(PostMetadata metadata, string content)>();

            var tagPosts = new Dictionary<string, (string urlPath, List<PostMetadata> metadatas)>();
            var tagCanonicalUrl = new Dictionary<string, Link>();

            var rawPosts = this.GetPosts(inputPath).ToList();

            foreach (var rawPost in rawPosts)
            {
                var (rawMetadata, rawContent) = this.SplitPost(rawPost);
                var metadatos = this.ConvertMetadata(rawMetadata);

                var mainRoute = rawSiteConfiguration.PostRoutes.First();
                (string _, string canonicalPath) = this.GetPostPaths(mainRoute, metadatos);
                metadatos.CanonicalUrl = canonicalPath;

                foreach (var tag in metadatos.TagArray)
                {
                    if (!tagPosts.ContainsKey(tag))
                    {
                        var (tagOutputPath, canonicalUrl) = this.GetPaths(this.siteConfiguration.TagPageRoute, tag);
                        tagCanonicalUrl[tag] = new Link { Url = canonicalUrl, Text = tag };
                        tagPosts[tag] = (tagOutputPath, new List<PostMetadata>());
                    }

                    tagPosts[tag].metadatas.Add(metadatos);
                }

                metadatos.Tags = metadatos.TagArray.Select(tag => tagCanonicalUrl[tag]).ToArray();

                postMetadata.Add((metadatos, rawContent));
            }

            this.WriteTags(inputPath, outputPath, tagPosts);

            this.WriteTagIndex(inputPath, outputPath, tagCanonicalUrl.Values);

            this.WritePosts(inputPath, outputPath, rawSiteConfiguration, postMetadata);

            this.WriteIndex(postMetadata.Select(element => element.metadata), inputPath, outputPath);
        }

        public virtual List<List<T>> SplitChunks<T>(List<T> values, int size = 30)
        {
            List<List<T>> list = new List<List<T>>();
            for (int i = 0; i < values.Count; i += size)
            {
                var finalSize = Math.Min(size, values.Count - i);
                var content = values.GetRange(i, finalSize);
                list.Add(content);
            }

            return list;
        }

        public virtual void WritePosts(string inputPath, string outputPath, Raw.SiteConfiguration rawSiteConfiguration, List<(PostMetadata metadata, string content)> postMetadata)
        {
            foreach (var (metadatos, content) in postMetadata)
            {
                var renderedPost = this.RenderContent(metadatos, content, inputPath);

                foreach (var tag in metadatos.TagArray)
                {
                    var (_, canonicalTag) = this.GetPaths(this.siteConfiguration.TagIndexRoute, tag);
                }

                foreach (string pathTemplate in rawSiteConfiguration.PostRoutes)
                {
                    (string filePath, string _) = this.GetPostPaths(pathTemplate, metadatos);
                    this.WriteFile(this.fileSystem.Path.Combine(outputPath, filePath), renderedPost);
                }
            }
        }

        public virtual void WriteTagIndex(string inputPath, string outputPath, IEnumerable<Link> tagPosts)
        {
            var fileText = this.fileSystem.File.ReadAllText(this.fileSystem.Path.Combine(inputPath, "templates", "_tag_index.liquid"));

            var (tagOutputPath, _) = this.GetPaths(this.siteConfiguration.TagIndexRoute, null);
            var renderVars = new
            {
                site = this.siteConfiguration,
                tags = tagPosts,
            };
            var renderedContent = this.liquidRenderer.Render(fileText, renderVars);
            this.WriteFile(this.fileSystem.Path.Combine(outputPath, tagOutputPath), renderedContent);
        }

        public virtual void WriteTags(string inputPath, string outputPath, Dictionary<string, (string urlPath, List<PostMetadata> metadatas)> tagPosts)
        {
            foreach (var (key, (outputFile, posts)) in tagPosts)
            {
                var fileText = this.fileSystem.File.ReadAllText(this.fileSystem.Path.Combine(inputPath, "templates", "_tag_page.liquid"));
                var renderVars = new
                {
                    site = this.siteConfiguration,
                    articles = posts,
                    tag_name = key,
                };
                var renderedContent = this.liquidRenderer.Render(fileText, renderVars);
                this.WriteFile(this.fileSystem.Path.Combine(outputPath, outputFile), renderedContent);
            }
        }

        public virtual void WriteIndex(IEnumerable<PostMetadata> posts, string rootPath, string outputPath)
        {
            var orderedPosts = posts.OrderBy(post => post.Date).Reverse().ToList();
            var splits = this.SplitChunks(orderedPosts, size: 10);

            for (int currentPage = 0; currentPage < splits.Count; currentPage++)
            {
                var paginator = this.CreatePaginator(splits, currentPage);

                var fileText = this.fileSystem.File.ReadAllText(this.fileSystem.Path.Combine(rootPath, "templates", "_index.liquid"));

                var renderVars = new
                {
                    site = this.siteConfiguration,
                    paginator = paginator,
                    posts = orderedPosts,
                };

                var rendered = this.liquidRenderer.Render(fileText, renderVars);
                using var stream = this.fileSystem.File.OpenWrite(this.fileSystem.Path.Combine(outputPath, paginator.CurrentPageUrl));
                using var file = new StreamWriter(stream);
                file.Write(rendered);
            }
        }

        public virtual Paginator CreatePaginator(List<List<PostMetadata>> splits, int currentPage)
        {
            var (previousIndex, index, nextIndex) = this.GenerateIndexNames(currentPage, splits.Count);

            return new Paginator()
            {
                PageCount = splits.Count,
                CurrentPage = currentPage,
                CurrentPageUrl = index,
                HasNextPage = nextIndex is not null,
                HasPreviousPage = previousIndex is not null,
                PreviousPage = currentPage - 1,
                NextPage = currentPage + 1,
                NextPageUrl = nextIndex,
                PreviousPageUrl = previousIndex,
                Posts = splits[currentPage],
            };
        }

        public virtual (string previousIndex, string currentIndex, string nextIndex) GenerateIndexNames(int currentPage, int pageCount)
        {
            var first = currentPage == 0;
            var last = currentPage == pageCount - 1;
            var index = currentPage == 0 ? "index.html" : $"index-{currentPage}.html";
            var previousIndex = $"index-{currentPage - 1}.html";
            var nextIndex = $"index-{currentPage + 1}.html";
            if (currentPage - 1 == 0)
            {
                previousIndex = "index.html";
            }

            return (first ? null : previousIndex, index, last ? null : nextIndex);
        }

        public virtual string RenderContent(PostMetadata metadata, string content, string inputPath)
        {
            var contentWrapped = new string[]
            {
                "{% extends post %}",
                "{% block post_content %}",
                this.markdownRenderer.RenderMarkdown(content),
                "{% endblock %}",
            };

            var postVariables = new
            {
                post = metadata,
                site = this.siteConfiguration,
            };

            var renderedContent = this.liquidRenderer.Render(string.Join('\n', contentWrapped), postVariables);

            return renderedContent;
        }

        public virtual PostMetadata ConvertMetadata(string metadata)
        {
            var rawMetadata = this.yamlParser.ParseExtras<Raw.PostMetadata>(metadata);
            return this.mapper.Map<PostMetadata>(rawMetadata);
        }

        public virtual (string metadata, string content) SplitPost(string post)
        {
            var metadatStringBulder = new StringBuilder();
            var contentStringBuilder = new StringBuilder();
            int separators = 0;
            const string separator = "---";
            foreach (var line in post.Split(Environment.NewLine))
            {
                if (separators == 2)
                {
                    contentStringBuilder.Append(line).AppendLine();
                }
                else if (line == separator)
                {
                    separators += 1;
                }
                else
                {
                    metadatStringBulder.Append(line).AppendLine();
                }
            }

            return (metadatStringBulder.ToString(), contentStringBuilder.ToString());
        }

        public virtual IEnumerable<string> GetPosts(string inputPath)
        {
            var inputPostsPath = this.fileSystem.Path.Combine(inputPath, "content", "posts");
            if (this.fileSystem.Directory.Exists(inputPostsPath))
            {
                foreach (var file in this.fileSystem.Directory.EnumerateFiles(inputPostsPath, "*.*", SearchOption.AllDirectories))
                {
                    yield return this.fileSystem.File.ReadAllText(file);
                }
            }
        }

        public virtual void CopyFiles(string input, string output)
        {
            var source = this.fileSystem.DirectoryInfo.FromDirectoryName(input);
            var target = this.fileSystem.DirectoryInfo.FromDirectoryName(output);

            this.CopyFiles(source, target);
        }

        public virtual void WriteFile(string filePath, string content)
        {
            var parentDirectory = this.fileSystem.Directory.GetParent(filePath);
            if (!parentDirectory.Exists)
            {
                parentDirectory.Create();
            }

            this.fileSystem.File.WriteAllText(filePath, content);
        }

        public virtual (string filePath, string canonicalPath) GetPostPaths(string pathTemplate, PostMetadata metadata)
        {
            var postSlug = this.slugifier.Slugify(metadata.Title);
            return this.GetPaths(pathTemplate, postSlug);
        }

        private (string filePath, string canonicalPath) GetPaths(string pathTemplate, string replaementValue)
        {
            pathTemplate = pathTemplate.Replace("{}", replaementValue).TrimStart('/');

            var filePath = pathTemplate.EndsWith(".html") ?
                pathTemplate : this.fileSystem.Path.Combine(pathTemplate, "index.html").Replace('\\', '/');

            var canonicalPath = pathTemplate.EndsWith("/index.html") ?
                pathTemplate.Substring(0, pathTemplate.Length - 11) : pathTemplate;

            return (filePath, $"/{canonicalPath}");
        }

        private Raw.SiteConfiguration ReadSiteConfiguration(string inputPath)
        {
            var source = this.fileSystem.Path.Combine(inputPath, "site.yml");
            var text = this.fileSystem.File.ReadAllText(source);

            var siteConf = this.yamlParser.Parse<Raw.SiteConfiguration>(text);

            // Set defaults
            siteConf.PostRoutes = siteConf.PostRoutes ?? new List<string> { "post/{}.html" };

            return siteConf;
        }

        private void CopyFiles(IDirectoryInfo source, IDirectoryInfo target)
        {
            foreach (var fi in source.GetFiles())
            {
                fi.CopyTo(this.fileSystem.Path.Combine(target.FullName, fi.Name));
            }

            foreach (var subDirectory in source.GetDirectories())
            {
                var nextTargetSubDir = target.CreateSubdirectory(subDirectory.Name);
                this.CopyFiles(subDirectory, nextTargetSubDir);
            }
        }
    }
}
