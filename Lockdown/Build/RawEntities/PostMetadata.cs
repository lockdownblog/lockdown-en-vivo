namespace Lockdown.Build.RawEntities
{
    using System;
    using YamlDotNet.Serialization;

    public class PostMetadata : EntityExtras
    {
        [YamlMember(Alias = "title")]
        public string Title { get; set; }

        [YamlMember(Alias = "date")]
        public DateTime Date { get; set; }
    }
}