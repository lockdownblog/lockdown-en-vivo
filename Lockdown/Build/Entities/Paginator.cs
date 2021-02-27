namespace Lockdown.Build.Entities
{
    using System.Collections;
    using System.Collections.Generic;
    using DotLiquid;

    public class Paginator : Drop, IEnumerable<PostMetadata>
    {
        public int CurrentPage { get; set; }

        public bool HasNextPage { get; set; }

        public bool HasPreviousPage { get; set; }

        public int NextPage { get; set; }

        public int PreviousPage { get; set; }

        public string NextPageUrl { get; set; }

        public string CurrentPageUrl { get; set; }

        public string PreviousPageUrl { get; set; }

        public int PageCount { get; set; }

        public List<PostMetadata> Posts { get; set; }

        public IEnumerator<PostMetadata> GetEnumerator()
        {
            foreach (var post in this.Posts)
            {
                yield return post;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (var post in this.Posts)
            {
                yield return post;
            }
        }
    }
}