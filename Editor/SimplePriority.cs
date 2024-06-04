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
            int bonus = 0;
            int score = 0;

            while (i < searchFilter.Length && j < name.Length)
            {
                if (result.ignoreCase)
                {
                    if (TSearchUtils.AreCharsEqualIgnoreCase(searchFilter[i], name[j]))
                    {
                        i++;
                        score += bonus;
                        bonus += 100;
                    }
                    else
                    {
                        bonus = 0;
                    }
                }
                else
                {
                    if (searchFilter[i] == name[j])
                    {
                        i++;
                        score += bonus;
                        bonus += 100;
                    }
                    else
                    {
                        bonus = 0;
                    }
                }

                j++;
            }

            score -= Mathf.Abs(i - searchFilter.Length);
            score -= Mathf.Abs(name.Length - searchFilter.Length);
            return score;
        }
    }
}