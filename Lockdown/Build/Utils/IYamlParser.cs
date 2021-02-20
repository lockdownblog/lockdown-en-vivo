namespace Lockdown.Build.Utils
{
    using Lockdown.Build.RawEntities;

    public interface IYamlParser
    {
        public T Parse<T>(string yamlString);

        public T ParseExtras<T>(string yamlString)
            where T : EntityExtras;
    }
}
