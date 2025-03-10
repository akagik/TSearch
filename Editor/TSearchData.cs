using System.Collections.Generic;
using UnityEditor;

namespace Room6.TSearch.Editor
{
    public class TSearchData : ScriptableSingleton<TSearchData>
    {
        public List<string>       allMenuCommands;
        public List<SearchResult> history = new();

        /// <summary>
        /// 従来の検索キーワード
        /// </summary>
        public string searchFilter = "";
        public string searchDir = "";

        /// <summary>
        /// 「in:Assets/MyFolder keyword...」のように、
        /// フォルダ指定ディレクティブも含めたフルの検索文字列
        /// </summary>
        public string fullSearchFilter = "";

        public int selectedTab = 0;

        public void CacheAllMenuCommands()
        {
            allMenuCommands = new List<string>();
            allMenuCommands.AddRange(TSearchUtils.GetAllMenuCommands());
        }

        // --- (1) fullSearchFilter から、folderPath と 検索キーワードを分解 ---
        // 例: "in:Assets/RPG Player" -> folderPath="Assets/RPG", searchKeyword="Player"
        //     "in:Assets "          -> folderPath="Assets",     searchKeyword=""
        //     "Player"              -> folderPath=null,         searchKeyword="Player" (=> デフォルト Assets)
        public (string folderPath, string keyword) ParseFullSearchFilter()
        {
            (searchDir, searchFilter) = ParseFullSearchFilter(fullSearchFilter);
            return (searchDir, searchFilter);
        }
        
        /// <summary>
        /// fullSearchFilter の先頭付近に "in:Some/Folder " のようなディレクティブがあれば取得し、
        /// 残りを検索キーワードとして返す。
        /// </summary>
        private static (string folderPath, string keyword) ParseFullSearchFilter(string _fullSearchFilter)
        {
            if (string.IsNullOrWhiteSpace(_fullSearchFilter))
            {
                // 空の場合: フォルダ指定なし, キーワードなし
                return ("Assets", "");
            }

            // まずトリム
            string s = _fullSearchFilter.Trim();

            // シンプルに最初が "in:" から始まるかどうかチェック
            if (s.StartsWith("in:"))
            {
                // 「in:」を除去
                s = s.Substring(3).TrimStart();
                // フォルダパスとしてスペースまでを切り出す
                // 例: "Assets/RPG Player" -> folder="Assets/RPG", 残り="Player"
                int spaceIndex = s.IndexOf(' ');
                if (spaceIndex >= 0)
                {
                    string folder = s.Substring(0, spaceIndex);
                    string keyword = s.Substring(spaceIndex + 1).Trim();
                    return (folder, keyword);
                }
                else
                {
                    // "in:Assets/RPG" のようにキーワードが無い場合
                    return (s, "");
                }
            }
            else
            {
                // "in:" が付いていない -> フォルダ指定なし, 検索キーワードのみとみなす
                return ("Assets", s);
            }
        }
    }
}