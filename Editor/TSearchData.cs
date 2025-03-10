using System.Collections.Generic;
using UnityEditor;

namespace Room6.TSearch.Editor
{
    public class TSearchData : ScriptableSingleton<TSearchData>
    {
        public List<string>       allMenuCommands;
        public List<SearchResult> history = new();

        /// <summary> 従来の検索キーワード(フォルダ指定を除いた部分) </summary>
        public string searchFilter = "";

        /// <summary> 前回検索に使用したフォルダパス </summary>
        public string searchDir = "";

        /// <summary>
        /// 「in:Assets/My\ Folder keyword...」のように、
        /// フォルダ指定ディレクティブも含めたフルの検索文字列
        /// </summary>
        public string fullSearchFilter = "";

        public int selectedTab = 0;

        public void CacheAllMenuCommands()
        {
            allMenuCommands = new List<string>();
            allMenuCommands.AddRange(TSearchUtils.GetAllMenuCommands());
        }

        /// <summary>
        /// fullSearchFilter から、folderPath と 検索キーワードを分解
        /// 例: 
        ///   "in:Assets/RPG Player"  -> folderPath="Assets/RPG", keyword="Player"
        ///   "in:Assets/My\ Folder NPC" -> folderPath="Assets/My Folder", keyword="NPC"
        ///   "Player"               -> folderPath="Assets",     keyword="Player" (=> デフォルト)
        /// </summary>
        public (string folderPath, string keyword) ParseFullSearchFilter()
        {
            (searchDir, searchFilter) = ParseFullSearchFilterWithEscape(fullSearchFilter);
            return (searchDir, searchFilter);
        }

        /// <summary>
        /// "in:～" ディレクティブ + バックスラッシュエスケープに対応したパース
        /// </summary>
        private static (string folderPath, string keyword) ParseFullSearchFilterWithEscape(string _fullSearchFilter)
        {
            if (string.IsNullOrWhiteSpace(_fullSearchFilter))
            {
                // 空の場合: フォルダ指定なし, キーワードなし => デフォルトで Assets
                return ("Assets", "");
            }

            string s = _fullSearchFilter.Trim();

            // 最初が "in:" ならフォルダ指定あり
            if (s.StartsWith("in:"))
            {
                // 「in:」部分を取り除く
                s = s.Substring(3).TrimStart();

                // バックスラッシュエスケープに対応して、最初の「アンエスケープ空白」までをフォルダパスとみなす
                var (folder, remainder) = ExtractFolderPath(s);
                string keyword = remainder.Trim();
                return (folder, keyword);
            }
            else
            {
                // "in:" が付いていなければフォルダ指定なし -> デフォルトは "Assets"
                return ("Assets", s);
            }
        }

        /// <summary>
        /// 文字列 s の先頭から、バックスラッシュでエスケープされていない最初の空白までを「folder path」として抜き出し、
        /// 残りを返す。
        /// 
        /// 例: 
        ///   s = "Assets/My\\ Folder NPC" 
        ///     -> folder="Assets/My Folder", remainder="NPC"
        /// </summary>
        private static (string folder, string remainder) ExtractFolderPath(string s)
        {
            var sb = new System.Text.StringBuilder();
            bool isEscaped = false;
            int i = 0;

            for (; i < s.Length; i++)
            {
                char c = s[i];
                if (isEscaped)
                {
                    // 直前がバックスラッシュの場合、そのまま文字を追加
                    sb.Append(c);
                    isEscaped = false;
                }
                else
                {
                    if (c == '\\')
                    {
                        // バックスラッシュ -> 次の文字をエスケープ
                        isEscaped = true;
                    }
                    else if (c == ' ')
                    {
                        // アンエスケープ空白に到達 -> ここでフォルダパス終了
                        break;
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
            }

            string folder = sb.ToString();
            string remainder = (i < s.Length) ? s.Substring(i + 1) : ""; 
            return (folder, remainder);
        }
    }
}
