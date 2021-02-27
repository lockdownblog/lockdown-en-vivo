namespace Lockdown.Build.RawEntities
{
    using YamlDotNet.Serialization;

    public class Link
    {
        [YamlMember(Alias = "name")]
        public string Text { get; set; }

        [YamlMember(Alias = "url")]
        public string Url { get; set; }
    }
}
