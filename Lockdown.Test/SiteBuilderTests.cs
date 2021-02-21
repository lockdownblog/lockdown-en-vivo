using Lockdown.Build;
using Raw = Lockdown.Build.RawEntities;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Xunit;
using Shouldly;
using System.Linq;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using AngleSharp.Dom;
using System.Threading.Tasks;
using AngleSharp;
using Lockdown.Build.Utils;
using Lockdown.Build.Entities;
using Lockdown.Build.Markdown;

namespace Lockdown.Test
{
    public class SiteBuilderTests
    {

        private readonly IFileSystem fakeFileSystem;
        private readonly Mock<IYamlParser> moqYamlParser;
        private readonly Mock<IMarkdownRenderer> moqMarkdownRenderer;
        private readonly Mock<ILiquidRenderer> moqLiquidRenderer;

        const string inputPath = "./input";
        const string output = "./output";


        public SiteBuilderTests()
        {
            fakeFileSystem = new MockFileSystem();
            moqYamlParser = new Mock<IYamlParser>();
            moqMarkdownRenderer = new Mock<IMarkdownRenderer>();
            moqLiquidRenderer = new Mock<ILiquidRenderer>();
        }

        [Fact]
        public void TestOutputFolderExist()
        {
            // Setup
            var fakeFilePath = fakeFileSystem.Path.Combine(output, "archivo.txt");
            fakeFileSystem.Directory.CreateDirectory(output);
            fakeFileSystem.File.WriteAllText(fakeFilePath, "hola mundo");

            var siteBuilder = new SiteBuilder(
                fakeFileSystem,
                moqYamlParser.Object,
                moqMarkdownRenderer.Object,
                moqLiquidRenderer.Object
            );

            // Act
            siteBuilder.CleanFolder(output);

            // Asserts
            this.AssertDirectoryIsEmpty(output);
        }

        [Fact]
        public void TestOutputFolderDoesNotExist()
        {
            // Setup
            var siteBuilder = new SiteBuilder(
                fakeFileSystem,
                moqYamlParser.Object,
                moqMarkdownRenderer.Object,
                moqLiquidRenderer.Object
            );

            // Act
            siteBuilder.CleanFolder(output);

            // Asserts
            this.AssertDirectoryIsEmpty(output);
        }

        /*
        [Fact]
        
        public void TestBuildCallsOtherMethods()
        {
            // Setup
            var mockSiteBuilder = new Mock<SiteBuilder>(MockBehavior.Strict, this.fakeFileSystem);
            mockSiteBuilder.Setup(sb => sb.CleanFolder(output));
            mockSiteBuilder.Setup(sb => sb.CopyFiles(inputPath, output));
            mockSiteBuilder.Setup(sb => sb.GetPosts(inputPath)).Returns(new string[0]);
            mockSiteBuilder.Setup(sb => sb.SplitPost(It.IsAny<string>())).Returns(Tuple.Create("",""));
            mockSiteBuilder.Setup(sb => sb.ConvertMetadata(It.IsAny<string>())).Returns(new RawPostMetadata());
            SiteBuilder siteBuilder = mockSiteBuilder.Object;

            // Act
            siteBuilder.Build(inputPath, output);

            // Assert
            mockSiteBuilder.Verify(sb => sb.CleanFolder(output));
            mockSiteBuilder.Verify(sb => sb.CopyFiles(inputPath, output));
        }
        */

        [Fact]
        public void TestCopyFiles()
        {
            // Setup
            var stylesFile = this.fakeFileSystem.Path.Combine(inputPath, "style.css");
            var someOtherFile = this.fakeFileSystem.Path.Combine(inputPath, "subfolder", "style.css");

            var contents = new Dictionary<string, MockFileData>
            {
                { stylesFile, new MockFileData("body { color: #fff; }") },
                { someOtherFile, new MockFileData("more data") }
            };

            var fakeFileSystem = new MockFileSystem(contents);
            fakeFileSystem.Directory.CreateDirectory(output);
            var siteBuilder = new SiteBuilder(
                fakeFileSystem,
                moqYamlParser.Object,
                moqMarkdownRenderer.Object,
                moqLiquidRenderer.Object
            );

            // Act
            siteBuilder.CopyFiles(inputPath, output);

            // Assert
            fakeFileSystem.Directory.EnumerateFiles(output, "*.*", SearchOption.AllDirectories).Count().ShouldBe(2);
        }

        private void AssertDirectoryIsEmpty(string output)
        {
            fakeFileSystem.Directory.Exists(output).ShouldBeTrue();
            fakeFileSystem.Directory.EnumerateFiles(output).Any().ShouldBeFalse();
            fakeFileSystem.Directory.EnumerateDirectories(output).Any().ShouldBeFalse();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        public void TestGetPostsWithSinglePost(int files)
        {
            var postsPath = this.fakeFileSystem.Path.Combine(inputPath, "content", "posts");
            this.fakeFileSystem.Directory.CreateDirectory(postsPath);
            var fileContents = new List<string>();
            for (var i = 0; i < files; i++)
            {
                var postPath = this.fakeFileSystem.Path.Combine(postsPath, $"file_{i}.txt");
                var content = "# Hola Mundo!\n\n**Prueba {i}**";
                this.fakeFileSystem.File.WriteAllText(postPath, content);
                fileContents.Add(content);
            }
            var siteBuilder = new SiteBuilder(
                fakeFileSystem,
                moqYamlParser.Object,
                moqMarkdownRenderer.Object,
                moqLiquidRenderer.Object
            );

            var posts = siteBuilder.GetPosts(inputPath);

            posts.OrderBy(content => content).ShouldBe(fileContents);
        }

        private async Task<IDocument> ParseHtml(string document)
        {
            var context = BrowsingContext.New(Configuration.Default);
            return await context.OpenAsync(req => req.Content(document));
        }

        [Fact]
        public async Task TestRenderContent()
        {
            // Setup: Prepare our test by copying our `demo` folder into our "fake" file system.
            var workspace = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "../../../../"));
            var templatePath = Path.Combine(workspace, "Lockdown", "BlankTemplate", "templates");
            var dictionary = new Dictionary<string, MockFileData>();
            foreach (var path in Directory.EnumerateFiles(templatePath))
            {
                var fakePath = path.Replace(templatePath, Path.Combine(inputPath, "templates"));
                dictionary.Add(fakePath, new MockFileData(File.ReadAllBytes(path)));
            }
            var fakeFileSystem = new MockFileSystem(dictionary);

            var metadata = new PostMetadata { Title = "Test post", Date = new DateTime(2000, 1, 1) };
            var postContent = "# Content #";
            var dotLiquidRenderer = new DotLiquidRenderer(fakeFileSystem);
            dotLiquidRenderer.SetRoot(inputPath);
            moqMarkdownRenderer.Setup(moq => moq.RenderMarkdown("# Content #")).Returns(() => "<b>Content</b>");
            var siteBuilder = new SiteBuilder(fakeFileSystem,
                moqYamlParser.Object,
                moqMarkdownRenderer.Object,
                dotLiquidRenderer
            );

            // Act
            var convertedPost = siteBuilder.RenderContent(metadata, postContent, inputPath);


            // Assert
            var html = await this.ParseHtml(convertedPost);

            var heading1 = html.All.First(node => node.LocalName == "h1");
            heading1.TextContent.ShouldBe("Test post");

            var bold = html.All.First(node => node.LocalName == "b");
            bold.TextContent.ShouldBe("Content");
        }
    }
}
