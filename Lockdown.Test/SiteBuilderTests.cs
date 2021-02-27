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
using AutoMapper;

namespace Lockdown.Test
{
    public class SiteBuilderTests
    {

        private readonly IFileSystem fakeFileSystem;
        private readonly ISlugifier slugifier;
        private readonly IMapper mapper;
        private readonly Mock<IYamlParser> moqYamlParser;
        private readonly Mock<IMarkdownRenderer> moqMarkdownRenderer;
        private readonly Mock<ILiquidRenderer> moqLiquidRenderer;
        private readonly SiteBuilder genericSiteBuilder;

        const string inputPath = "./input";
        const string output = "./output";


        public SiteBuilderTests()
        {
            fakeFileSystem = new MockFileSystem();
            moqYamlParser = new Mock<IYamlParser>();
            moqMarkdownRenderer = new Mock<IMarkdownRenderer>();
            moqLiquidRenderer = new Mock<ILiquidRenderer>();
            slugifier = new Slugifier();
            mapper = Build.Mapping.Mapper.GetMapper();
            genericSiteBuilder = new SiteBuilder(
                fakeFileSystem,
                moqYamlParser.Object,
                moqMarkdownRenderer.Object,
                moqLiquidRenderer.Object,
                slugifier,
                mapper
            );
        }

        [Fact]
        public void TestOutputFolderExist()
        {
            // Setup
            var fakeFilePath = fakeFileSystem.Path.Combine(output, "archivo.txt");
            fakeFileSystem.Directory.CreateDirectory(output);
            fakeFileSystem.File.WriteAllText(fakeFilePath, "hola mundo");


            // Act
            genericSiteBuilder.CleanFolder(output);

            // Asserts
            this.AssertDirectoryIsEmpty(output);
        }

        [Fact]
        public void TestOutputFolderDoesNotExist()
        {
            // Act
            genericSiteBuilder.CleanFolder(output);

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
                moqLiquidRenderer.Object,
                slugifier,
                mapper
            );

            // Act
            siteBuilder.CopyFiles(inputPath, output);

            // Assert
            fakeFileSystem.Directory.EnumerateFiles(output, "*.*", SearchOption.AllDirectories).Count().ShouldBe(2);
        }

        [Fact]
        public void TestWriteFile()
        {
            var destination = fakeFileSystem.Path.Combine(inputPath, "some", "folder", "file.txt");
            var content = "Hello world!";

            // Act
            genericSiteBuilder.WriteFile(destination, content);

            // Assert
            fakeFileSystem.File.ReadAllText(destination).ShouldBe(content);
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

            var posts = genericSiteBuilder.GetPosts(inputPath);

            posts.OrderBy(content => content).ShouldBe(fileContents);
        }

        [Theory]
        [InlineData("/{}.html", "hello-world.html", "/hello-world.html")]
        [InlineData("{}.html", "hello-world.html", "/hello-world.html")]
        [InlineData("/{}", "hello-world/index.html", "/hello-world")]
        [InlineData("/post/{}", "post/hello-world/index.html", "/post/hello-world")]
        [InlineData("/post/{}/index.html", "post/hello-world/index.html", "/post/hello-world")]
        public void TestGetRoutes(string template, string fileExpected, string canonicalExpected)
        {
            var metadata = new PostMetadata { Title = "Hello World" };

            var (filePath, canonicalPath) = genericSiteBuilder.GetPostPaths(template, metadata);

            filePath.ShouldBe(fileExpected);
            canonicalPath.ShouldBe(canonicalExpected);
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
                dotLiquidRenderer,
                slugifier,
                mapper
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


        [Theory]
        [InlineData(0, 1, null, "index.html", null)]
        [InlineData(0, 2, null, "index.html", "index-1.html")]
        [InlineData(1, 2, "index.html", "index-1.html", null)]
        [InlineData(1, 3, "index.html", "index-1.html", "index-2.html")]
        public void TestGenerateIndexNames(int currentPage, int pageCount, string previous, string current, string next)
        {
            var (actualPrevious, actualCurrent, actualNext) = genericSiteBuilder.GenerateIndexNames(currentPage, pageCount);

            Assert.Equal(previous, actualPrevious);
            Assert.Equal(next, actualNext);
            actualCurrent.ShouldBe(current);
        }

        [Theory]
        [InlineData(10, 2, 2)]
        [InlineData(9, 2, 1)]
        [InlineData(1, 2, 1)]
        [InlineData(1, 1, 1)]
        public void SplitChunks(int totalSize, int chunkSize, int lastSize)
        {
            var collection = Enumerable.Range(0, totalSize).ToList();

            var chunks = genericSiteBuilder.SplitChunks(collection, chunkSize);

            chunks.Last().Count().ShouldBe(lastSize);
            foreach (var chunk in chunks.SkipLast(1))
            {
                chunk.Count().ShouldBe(chunkSize);
            }
        }
    }
}
