using System;
using System.Threading.Tasks;
using Lockdown.Build.Utils;
using Xunit;
using Raw = Lockdown.Build.RawEntities;
using Shouldly;
using Lockdown.Build.Markdown;

namespace Lockdown.Test
{
    public class MarkdownRendererTests
    {

        MarkdownRenderer markdownRenderer;
        public MarkdownRendererTests()
        {
            this.markdownRenderer = new MarkdownRenderer();
        }

        [Fact]
        public void TestParseSimple()
        {
            var md = @"# Hello world";

            var html = this.markdownRenderer.RenderMarkdown(md);

            html.ShouldBe("<h1>Hello world</h1>\n");
        }
    }
}
