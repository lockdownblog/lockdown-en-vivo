namespace Lockdown.Build.RawEntities
{
    using System.Collections.Generic;
    using YamlDotNet.Serialization;

    public class SiteConfiguration
    {
        [YamlMember(Alias = "title")]
        public string Title { get; set; }

        [YamlMember(Alias = "subtitle")]
        public string Subtitle { get; set; }

        [YamlMember(Alias = "description")]
        public string Description { get; set; }

        [YamlMember(Alias = "site-url")]
        public string SiteUrl { get; set; }

        [YamlMember(Alias = "default-author")]
        public string DefaultAuthor { get; set; }

        [YamlMember(Alias = "pages-in-tags")]
        public bool PagesInTags { get; set; }

        [YamlMember(Alias = "social")]
        public List<Link> Social { get; set; }

        [YamlMember(Alias = "post-routes")]
        public List<string> PostRoutes { get; set; }

        [YamlMember(Alias = "tag-index-route")]
        public string TagIndexRoute { get; set; }

        [YamlMember(Alias = "tag-page-route")]
        public string TagPageRoute { get; set; }
    }
}
