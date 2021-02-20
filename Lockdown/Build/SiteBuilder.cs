namespace Lockdown.Build
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text;
    using DotLiquid;
    using Lockdown.Build.Entities;
    using Lockdown.Build.Markdown;
    using Lockdown.Build.Utils;
    using Slugify;
    using Raw = Lockdown.Build.RawEntities;

    public class SiteBuilder : ISiteBuilder
    {
        private readonly IFileSystem fileSystem;
        private readonly IYamlParser yamlParser;
        private readonly IMarkdownRenderer markdownRenderer;

        private SiteConfiguration siteConfiguration;

        public SiteBuilder(
            IFileSystem fileSystem,
            IYamlParser yamlParser,
            IMarkdownRenderer markdownRenderer)
        {
            this.fileSystem = fileSystem;
            this.yamlParser = yamlParser;
            this.markdownRenderer = markdownRenderer;
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

            this.siteConfiguration = this.ReadSiteConfiguration(inputPath);

            var rawPosts = this.GetPosts(inputPath);
            var slugHelper = new SlugHelper();
            foreach (var rawPost in rawPosts)
            {
                var post = this.SplitPost(rawPost);
                var metadatos = this.ConvertMetadata(post.Item1);
                var contenidoCadena = post.Item2;

                var renderedPost = this.RenderContent(metadatos, contenidoCadena, inputPath);

                var postSlug = slugHelper.GenerateSlug(metadatos.Title);
                var outputPostPath = this.fileSystem.Path.Combine(outputPath, $"{postSlug}.html");

                this.fileSystem.File.WriteAllText(outputPostPath, renderedPost);
            }
        }

        public virtual string RenderContent(PostMetadata metadata, string content, string inputPath)
        {
            Template.FileSystem = new HelperFileSystem(this.fileSystem, inputPath);

            var contentWrapped = new string[]
            {
                "{% extends post %}",
                "{% block post_content %}",
                this.markdownRenderer.RenderMarkdown(content),
                "{% endblock %}",
            };

            var template = Template.Parse(string.Join('\n', contentWrapped));

            var postVariables = new
            {
                post = metadata,
                site = this.siteConfiguration,
            };

            var renderedContent = template.Render(Hash.FromAnonymousObject(postVariables));

            return renderedContent;
        }

        public virtual PostMetadata ConvertMetadata(string metadata)
        {
            var rawMetadata = this.yamlParser.ParseExtras<Raw.PostMetadata>(metadata);

            return new PostMetadata
            {
                Title = rawMetadata.Title,
                Date = rawMetadata.Date,
            };
        }

        public virtual Tuple<string, string> SplitPost(string post)
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

            return Tuple.Create(
                metadatStringBulder.ToString(),
                contentStringBuilder.ToString());
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

        private SiteConfiguration ReadSiteConfiguration(string inputPath)
        {
            var source = this.fileSystem.Path.Combine(inputPath, "site.yml");
            var text = this.fileSystem.File.ReadAllText(source);

            var rawMetadata = this.yamlParser.Parse<Raw.SiteConfiguration>(text);

            return new SiteConfiguration
            {
                Title = rawMetadata.Title,
                Subtitle = rawMetadata.Subtitle,
                Description = rawMetadata.Description,
                SiteUrl = rawMetadata.SiteUrl,
                DefaultAuthor = rawMetadata.DefaultAuthor,

                Social = rawMetadata.Social.Select(lk => new SocialLink { Link = lk.Link, Name = lk.Name }).ToList(),
            };
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
