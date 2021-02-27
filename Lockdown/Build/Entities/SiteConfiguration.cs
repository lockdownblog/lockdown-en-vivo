namespace Lockdown.Build.Entities
{
    using System.Collections.Generic;
    using DotLiquid;

    public class SiteConfiguration : Drop
    {
        public string Title { get; set; }

        public string Subtitle { get; set; }

        public string DefaultAuthor { get; set; }

        public bool PagesInTags { get; set; }

        public string Description { get; set; }

        public string SiteUrl { get; set; }

        public List<Link> Social { get; set; }

        public List<string> PostRoutes { get; set; }

        public string TagIndexRoute { get; set; }

        public string TagPageRoute { get; set; }
    }
}
