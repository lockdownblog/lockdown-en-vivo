namespace Lockdown.Build.RawEntities
{
    using System;
    using System.Linq;
    using YamlDotNet.Serialization;

    public class PostMetadata : EntityExtras
    {
        [YamlMember(Alias = "title")]
        public string Title { get; set; }

        [YamlMember(Alias = "date")]
        public DateTime? Date { get; set; }

        [YamlMember(Alias = "tags")]
        public string Tags { get; set; }

        [YamlIgnore]
        public string[] TagArray => this.Tags?
            .Split(",", StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .ToArray() ?? new string[0];
    }
}