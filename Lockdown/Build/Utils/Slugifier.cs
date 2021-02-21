namespace Lockdown.Build.Utils
{
    using Slugify;

    public class Slugifier : ISlugifier
    {
        private readonly SlugHelper slugHelper;

        public Slugifier()
        {
            this.slugHelper = new SlugHelper();
        }

        public string Slugify(string text)
        {
            return this.slugHelper.GenerateSlug(text);
        }
    }
}
