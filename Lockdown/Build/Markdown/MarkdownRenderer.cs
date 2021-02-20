namespace Lockdown.Build.Markdown
{
    using System.IO;
    using Markdig;
    using Markdig.Renderers;

    public class MarkdownRenderer : IMarkdownRenderer
    {
        public string RenderMarkdown(string text)
        {
            var document = Markdown.Parse(text);

            using var writer = new StringWriter();
            var htmlRenderer = new HtmlRenderer(writer);

            foreach (var documentPart in document)
            {
                htmlRenderer.Write(documentPart);
            }

            return writer.ToString();
        }
    }
}