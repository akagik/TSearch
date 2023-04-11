using UnityEngine;

namespace Room6.TSearch.Editor
{
    public class SimplePriority : Priority
    {
        public override int GetPriority(SearchResult result, string searchFilter)
        {
            string name = result.fileName;
            
            int i = 0;
            int j = 0;

            while (i < searchFilter.Length && j < name.Length)
            {
                if (result.ignoreCase)
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

            return - Mathf.Abs(i - searchFilter.Length) - Mathf.Abs(name.Length - searchFilter.Length);
        }
    }
}