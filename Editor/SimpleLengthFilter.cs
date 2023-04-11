namespace Room6.TSearch.Editor
{
    public class SimpleLengthFilter : SearchFilter
    {
        public override bool Filter(SearchResult result, string filter)
        {
            // if (totalLength > 100)
            // {
            //     return searchFilter.Length + 3 > name.Length;
            // }
            //
            return true;
        }
    }
}