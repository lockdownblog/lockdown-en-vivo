﻿namespace Lockdown.Build
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

            var rawPosts = this.GetPosts(inputPath);

            foreach (var rawPost in rawPosts)
            {
                var (rawMetadata, rawContent) = this.SplitPost(rawPost);
                var metadatos = this.ConvertMetadata(rawMetadata);

                var mainRoute = rawSiteConfiguration.PostRoutes.First();

                (string _, string canonicalPath) = this.GetPaths(mainRoute, metadatos);

                metadatos.CanonicalUrl = canonicalPath;

                var renderedPost = this.RenderContent(metadatos, rawContent, inputPath);

                foreach (string pathTemplate in rawSiteConfiguration.PostRoutes)
                {
                    (string filePath, string _) = this.GetPaths(pathTemplate, metadatos);
                    this.WriteFile(this.fileSystem.Path.Combine(outputPath, filePath), renderedPost);
                }
            }
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

        public virtual (string filePath, string canonicalPath) GetPaths(string pathTemplate, PostMetadata metadata)
        {
            var postSlug = this.slugifier.Slugify(metadata.Title);
            pathTemplate = pathTemplate.Replace("{}", postSlug).TrimStart('/');

            var filePath = pathTemplate.EndsWith(".html") ?
                pathTemplate : this.fileSystem.Path.Combine(pathTemplate, "index.html").Replace('\\', '/');

            var canonicalPath = pathTemplate.EndsWith("/index.html") ?
                pathTemplate.Substring(0, pathTemplate.Length - 11) : pathTemplate;

            return (filePath, canonicalPath);
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
