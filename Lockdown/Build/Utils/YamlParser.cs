namespace Lockdown.Build.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;
    using Lockdown.Build.RawEntities;
    using YamlDotNet.Serialization;

    public class YamlParser : IYamlParser
    {
        private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
    .IgnoreUnmatchedProperties()
    .Build();

        public YamlParser()
        {
        }

        public T Parse<T>(string yamlString)
        {
            return YamlDeserializer.Deserialize<T>(yamlString);
        }

        public T ParseExtras<T>(string yamlString)
            where T : EntityExtras
        {
            var original = YamlDeserializer.Deserialize<T>(yamlString);
            var metadataEntries = yamlString
                .Split(Environment.NewLine)
                .Where(line => !string.IsNullOrEmpty(line))
                .Where(line => !line.StartsWith(' ') && !line.StartsWith('\t'))
                .Select(line => line.Split(':', 2))
                .Select(parts => KeyValuePair.Create(parts[0].Trim().ToLower(), parts[1].Trim()));

            var dict = new Dictionary<string, string>(metadataEntries);

            original.Extras = new DynamicDictionary(dict);

            return original;
        }
    }
}
