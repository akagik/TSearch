using System;
using System.Collections.Generic;
using System.Linq;
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
        
        // ◆ 全アセットをキャッシュするためのリストや辞書
        [NonSerialized]
        private List<string> allGuids;
        [NonSerialized]
        private Dictionary<string, string> guidToPath;

        // キャッシュが更新されているかどうか
        [NonSerialized]
        private bool isAssetCacheBuilt = false;

        /// <summary>
        /// プロジェクト内の全アセット GUID をキャッシュする。
        /// </summary>
        public void BuildAssetCacheIfNeeded()
        {
            if (isAssetCacheBuilt) return;

            // プロジェクト内の全アセット GUID を一度取得
            // 大規模プロジェクトでは時間がかかるため、基本的に一度だけ行う
            string[] guids = AssetDatabase.FindAssets("");
            // 重複排除
            allGuids = guids.Distinct().ToList();

            // GUID -> Path マッピングを作っておく
            guidToPath = new Dictionary<string, string>(allGuids.Count);
            foreach (var guid in allGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                guidToPath[guid] = path;
            }

            isAssetCacheBuilt = true;
        }
        
        /// <summary>
        /// フォルダ指定付きで、キャッシュされた GUID 一覧を絞り込む。
        /// </summary>
        /// <param name="folderPath">in:フォルダ指定があればそのパス。無ければ null/空文字</param>
        /// <returns></returns>
        public IEnumerable<string> GetCachedGuidsFilteredByFolder(string folderPath)
        {
            // キャッシュ未生成なら生成
            BuildAssetCacheIfNeeded();

            // フォルダ指定無しなら全 GUID を返す
            if (string.IsNullOrEmpty(folderPath) || folderPath == "Assets")
            {
                return allGuids;
            }

            // たとえば、folderPath = "Assets/SubFolder" の場合
            // パスが "Assets/SubFolder" で始まっていれば対象にする
            return allGuids.Where(guid =>
            {
                string path = guidToPath[guid];
                return path.StartsWith(folderPath, StringComparison.OrdinalIgnoreCase);
            });
        }

        /// <summary>
        /// GUID からパスを取得
        /// </summary>
        public string GetAssetPathFromGuid(string guid)
        {
            if (guidToPath != null && guidToPath.TryGetValue(guid, out var path))
            {
                return path;
            }
            return AssetDatabase.GUIDToAssetPath(guid); // キャッシュに無い場合(基本的に無いはず)
        }
    }
}
