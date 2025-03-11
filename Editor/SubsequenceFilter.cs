namespace Room6.TSearch.Editor
{
    public class SubsequenceFilter : SearchFilter
    {
        public override bool Filter(string name, string searchFilter, bool ignoreCase)
        {
            if (name.Length < searchFilter.Length)
            {
                return false;
            }

            int i = 0;
            int j = 0;

            while (i < searchFilter.Length && j < name.Length)
            {
                if (ignoreCase)
                {
                    if (TSearchUtils.AreCharsEqualIgnoreCase(searchFilter[i], name[j]))
                    {
                        i++;
                    }
                }
                else
                {
                    if (searchFilter[i] == name[j])
                    {
                        i++;
                    }
                }

                j++;
            }

            return i == searchFilter.Length;
        }

        public override bool Filter(SearchResult result, string searchFilter)
        {
            string name = result.fileName;
            return Filter(name, searchFilter, result.ignoreCase);
        }
    }
}