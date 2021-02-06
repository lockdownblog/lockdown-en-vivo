namespace Lockdown.Build
{
    using DotLiquid;
    using Slugify;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text;

    public class SiteBuilder : ISiteBuilder
    {
        private readonly IFileSystem fileSystem;

        public SiteBuilder(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        public virtual void CleanFolder(string folder)
        {
            if (this.fileSystem.Directory.Exists(folder))
            {
                this.fileSystem.Directory.Delete(folder, recursive: true);
            }

            this.fileSystem.Directory.CreateDirectory(folder);
        }

        public virtual void CopyFiles(string input, string output)
        {
            var source = this.fileSystem.DirectoryInfo.FromDirectoryName(input);
            var target = this.fileSystem.DirectoryInfo.FromDirectoryName(output);

            this.CopyFiles(source, target);
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

        public void Build(string inputPath, string outputPath)
        {
            this.CleanFolder(outputPath);
            this.CopyFiles(inputPath, outputPath);

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

        public virtual string RenderContent(RawPostMetadata metadata, string content, string inputPath)
        {
            Template.FileSystem = new HelperFileSystem(this.fileSystem, inputPath);

            var contentWrapped = @"{% extends post %}
{% block post_content %}
" + content + @"
{% endblock %}";

            var template = Template.Parse(contentWrapped);

            var postVariables = new
            {
                title = metadata.Title,
                date = metadata.Date,
            };

            var renderedContent = template.Render(Hash.FromAnonymousObject(postVariables));

            return renderedContent;
        }

        public virtual RawPostMetadata ConvertMetadata(string metadata)
        {
            var metadataEntries = metadata
                .Split(Environment.NewLine)
                .Where(line => !string.IsNullOrEmpty(line))
                .Select(line => line.Split(':', 2))
                .Select(parts => KeyValuePair.Create(parts[0].Trim().ToLower(), parts[1].Trim()));

            var dict = new Dictionary<string, string>(metadataEntries);

            return new RawPostMetadata
            {
                Title = dict["title"],
                Date = DateTime.Parse(dict["date"]),
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
            var inputPostsPath = this.fileSystem.Path.Combine(inputPath, "posts");
            if (this.fileSystem.Directory.Exists(inputPostsPath))
            {
                foreach (var file in this.fileSystem.Directory.EnumerateFiles(inputPostsPath, "*.*", SearchOption.AllDirectories))
                {
                    yield return this.fileSystem.File.ReadAllText(file);
                }
            }
        }
    }
}
