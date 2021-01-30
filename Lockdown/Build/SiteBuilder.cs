namespace Lockdown.Build
{
    using System.IO.Abstractions;

    public class SiteBuilder : ISiteBuilder
    {
        private readonly IFileSystem fileSystem;

        public SiteBuilder(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        public void CleanFolder(string folder)
        {

        }

        public void Build(string inputPath, string outputPath)
        {
            this.CleanFolder(outputPath);
        }
    }
}
