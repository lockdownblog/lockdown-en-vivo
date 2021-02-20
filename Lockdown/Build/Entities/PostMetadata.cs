namespace Lockdown.BuildEntities
{
    using System;
    using DotLiquid;

    public class PostMetadata : Drop
    {
        public string Title { get; set; }

        public DateTime Date { get; set; }
    }
}