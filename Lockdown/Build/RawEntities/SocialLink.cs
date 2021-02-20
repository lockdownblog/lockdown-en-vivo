namespace Lockdown.Build.RawEntities
{
    using YamlDotNet.Serialization;

    public class SocialLink
    {
        [YamlMember(Alias = "name")]
        public string Name { get; set; }

        [YamlMember(Alias = "link")]
        public string Link { get; set; }
    }
}
