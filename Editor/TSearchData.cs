using System.Collections.Generic;
using UnityEditor;

namespace Room6.TSearch.Editor
{
    public class TSearchData : ScriptableSingleton<TSearchData>
    {
        public List<string>       allMenuCommands;
        public List<SearchResult> history = new();

        public string searchFilter = "";
        public int    selectedTab  = 0;

        public void CacheAllMenuCommands()
        {
            allMenuCommands = new List<string>();
            allMenuCommands.AddRange(TSearchUtils.GetAllMenuCommands());
        }
    }
}