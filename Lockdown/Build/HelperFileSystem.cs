namespace Lockdown.Build
{
    using System.IO.Abstractions;
    using DotLiquid;

    public class HelperFileSystem : DotLiquid.FileSystems.IFileSystem
    {
        private readonly IFileSystem fileSystem;
        private string rootPath;

        public HelperFileSystem(IFileSystem fileSystem, string rootPath)
        {
            this.fileSystem = fileSystem;
            this.rootPath = rootPath;
        }

        public string ReadTemplateFile(Context context, string templateName)
        {
            var templatePath = this.fileSystem.Path.Combine(this.rootPath, "templates", $"_{templateName}.liquid");
            return this.fileSystem.File.ReadAllText(templatePath);
        }
    }
}
