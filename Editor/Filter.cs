namespace Room6.TSearch.Editor
{
    public abstract class SearchFilter
    {
        public abstract bool Filter(string name, string filter, bool ignoreCase);
        public abstract bool Filter(SearchResult result, string filter);
    }
}