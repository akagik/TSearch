namespace Room6.TSearch.Editor
{
    public abstract class Priority
    {
        public abstract int GetPriority(SearchResult result, string searchFilter);
    }
}