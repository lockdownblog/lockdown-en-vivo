namespace Lockdown.Build.Entities
{
    using System;
    using DotLiquid;

    public class PostMetadata : Drop
    {
        public string Title { get; set; }

        public DateTime Date { get; set; }

        public Link[] Tags { get; set; }

        public string[] TagArray { get; set; }

        public string CanonicalUrl { get; set; }
    }
}