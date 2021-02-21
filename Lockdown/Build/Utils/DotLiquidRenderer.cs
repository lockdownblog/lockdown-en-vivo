namespace Lockdown.Build.Utils
{
    using System.IO.Abstractions;
    using DotLiquid;

    public class DotLiquidRenderer : ILiquidRenderer
    {
        private readonly IFileSystem fileSystem;

        public DotLiquidRenderer(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        public string Render(string content, object variables)
        {
            var template = Template.Parse(content);
            return template.Render(Hash.FromAnonymousObject(variables));
        }

        public void SetRoot(string root)
        {
            Template.FileSystem = new HelperFileSystem(this.fileSystem, root);
        }
    }
}
