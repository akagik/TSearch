using UnityEngine;

namespace Room6.TSearch.Editor
{
    public class LevenshteinPriority : Priority
    {
        public override int GetPriority(SearchResult result, string searchFilter)
        {
            string name = result.fileName;
            
            int score = 0;
            int maxScore = Mathf.Min(searchFilter.Length, name.Length);

            int[,] matrix = new int[name.Length + 1, searchFilter.Length + 1];
            for (int i = 0; i <= name.Length; i++)
            {
                matrix[i, 0] = i;
            }

            for (int j = 0; j <= searchFilter.Length; j++)
            {
                matrix[0, j] = j;
            }

            for (int i = 1; i <= name.Length; i++)
            {
                for (int j = 1; j <= searchFilter.Length; j++)
                {
                    int cost = (name[i - 1] == searchFilter[j - 1]) ? 0 : 1;
                    matrix[i, j] = Mathf.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1, matrix[i - 1, j - 1] + cost);
                }
            }

            score = maxScore - matrix[name.Length, searchFilter.Length];

            if (name.StartsWith(searchFilter, System.StringComparison.OrdinalIgnoreCase))
            {
                score += 2;
            }

            return score;
        }
    }
}