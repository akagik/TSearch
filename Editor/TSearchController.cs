using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Room6.TSearch.Editor
{
    [System.Serializable]
    public class TSearchController
    {
        public static readonly string[] TabNames =
            { "Assets", "Hierarchy", "TextInHierarchy", "MenuCommand", "History" };

        public IEnumerable<SearchResult> searchResults; // 全検索結果
        public List<SearchResult> filteredResult = new(); // 表示用にフィルタされた結果
        public int totalLength;
        public Priority priorityCalculator = new SimplePriority();
        // ここでサブシーケンス検索を行うためのFilterを利用
        public SearchFilter searchResultFilter1 = new SimpleLengthFilter();
        public SearchFilter searchResultFilter2 = new SubsequenceFilter();
        public SearchResult activeResult { get; protected set; }
        public int activeIndex { get; protected set; } = -1;

        public CancellationTokenSource cancellationTokenSource;

        public TSearchData data => TSearchData.instance;

        // 検索用
        bool ignoreCase;
        string filterWithoutExtension;
        string filterExtension;
        bool hasExtension;

        public void OnEnable()
        {
            data.CacheAllMenuCommands();

            if (cancellationTokenSource == null)
            {
                cancellationTokenSource = new CancellationTokenSource();
            }

            // ウィンドウを開いたタイミング(あるいは再オープン時)に、
            string folder = TSearchUtils.GetSelectedFolderPathOrDefault();
            if (string.IsNullOrEmpty(data.searchFilter) || !folder.Equals(data.searchDir))
            {
                if (!folder.Equals(data.searchDir))
                {
                    if (folder != "Assets")
                    {
                        data.fullSearchFilter = $"in:{folder} ";
                    }
                    else
                    {
                        data.fullSearchFilter = "";
                    }
                }
            }

            // ウィンドウを開いたときに自動検索
            SearchAsyncWrapper(cancellationTokenSource.Token).Forget();
        }

        public void OnActiveMoved(bool isUp)
        {
            int direction = isUp ? -1 : 1;

            if (filteredResult.Count == 0)
            {
                activeIndex = -1;
                activeResult = null;
                return;
            }

            activeIndex = (activeIndex + direction + filteredResult.Count) % filteredResult.Count;
            activeResult = filteredResult[activeIndex];

            // 必要に応じて Ping
            EditorGUIUtility.PingObject(activeResult.asset);
            Selection.activeObject = activeResult.asset;
        }

        public void ResetActive()
        {
            activeIndex = 0;
            activeResult = null;
        }

        /// <summary>
        /// タブを次へ/前へ切り替える
        /// </summary>
        public void ChangeTabNext(int direction)
        {
            ResetActive();
            data.selectedTab = (data.selectedTab + direction + TabNames.Length) % TabNames.Length;
            OnTabChanged();
        }

        /// <summary>
        /// タブが変更された時に呼ばれる
        /// </summary>
        public void OnTabChanged()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = new CancellationTokenSource();
            SearchAsyncWrapper(cancellationTokenSource.Token).Forget();
        }

        public void ClearSearch()
        {
            data.history.Clear();
            data.searchFilter = "";
            filteredResult.Clear();
            searchResults = null;
        }

        public void OnSearchChanged(string newSearchFilter)
        {
            if (newSearchFilter != data.searchFilter)
            {
                data.searchFilter = newSearchFilter;
                cancellationTokenSource?.Cancel();
                cancellationTokenSource = new CancellationTokenSource();
                SearchAsyncWrapper(cancellationTokenSource.Token).Forget();
            }
        }

        public void AddHistory(SearchResult result)
        {
            data.history.Remove(result);
            data.history.Insert(0, result);
        }

        public void JumpToAsset(SearchResult result)
        {
            if (result.resultType == ResultType.MenuCommand)
            {
                return;
            }

            EditorGUIUtility.PingObject(result.asset);
            AddHistory(result);
        }

        public void Execute(SearchResult result)
        {
            result.Execute();
            AddHistory(result);
        }

        public void MoveTo(SearchResult result, UnityEngine.Object[] selectedObjects)
        {
            if (!result.IsDirectory)
            {
                return;
            }

            string folderPath = result.assetPath;

            foreach (UnityEngine.Object selectedObject in selectedObjects)
            {
                string selectedAssetPath = AssetDatabase.GetAssetPath(selectedObject);
                if (string.IsNullOrEmpty(selectedAssetPath)) continue;

                string selectedFolderPath = Path.GetDirectoryName(selectedAssetPath);
                if (selectedFolderPath == folderPath) continue;

                AssetDatabase.MoveAsset(selectedAssetPath,
                    Path.Combine(folderPath, Path.GetFileName(selectedAssetPath)));
            }

            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(result.asset);
        }

        /// <summary>
        /// 非同期で検索を実行するためのラッパ
        /// </summary>
        public async UniTask SearchAsyncWrapper(CancellationToken cancellationToken)
        {
            try
            {
                await SearchAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // キャンセルされた場合は何もしない
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                cancellationTokenSource = null;
            }

            // 検索完了後にウィンドウを Repaint
            EditorWindow.GetWindow<TSearchEditorWindow>("TSearch").Repaint();
        }

        /// <summary>
        /// 選択しているフォルダがあれば、そのフォルダを返す。なければ "Assets" を返す。
        /// </summary>
        private string[] GetSelectedFolderPathsOrDefault()
        {
            var objs = Selection.GetFiltered<DefaultAsset>(SelectionMode.Assets);
            var folderPaths = new List<string>();
            foreach (var obj in objs)
            {
                var path = AssetDatabase.GetAssetPath(obj);
                // フォルダかどうか
                if (AssetDatabase.IsValidFolder(path))
                {
                    folderPaths.Add(path);
                }
            }

            // もしフォルダ選択がなければルート("Assets")で検索
            if (folderPaths.Count == 0)
            {
                folderPaths.Add("Assets");
            }

            return folderPaths.ToArray();
        }

        /// <summary>
        /// 選択タブに応じて必要な検索を行う
        /// </summary>
        private async UniTask SearchAsync(CancellationToken token)
        {
            activeIndex = 0;
            activeResult = null;
            filteredResult.Clear();

            string currentTabName = TabNames[data.selectedTab];
            (string folderPath, string leftover) = data.ParseFullSearchFilter();

            // ここで leftover を data.searchFilter に代入（タブ切り替えなどで使う可能性があるため）
            data.searchFilter = leftover;

            if (currentTabName == "Assets")
            {
                // スペース区切りで入力された leftover をトークン分割
                string[] tokens = leftover
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                // キーワード入力が無い場合（in:◯◯だけ）なら結果ゼロ
                if (tokens.Length == 0)
                {
                    searchResults = Enumerable.Empty<SearchResult>();
                    totalLength = 0;
                    return;
                }

                // 最後のトークンだけファイル名検索に使う
                string fileNameKeyword = tokens[^1];
                // 残りのトークンはフォルダ名検索に使う
                string[] folderNameKeywords = tokens.Length > 1
                    ? tokens[..^1]
                    : Array.Empty<string>();

                // 大文字/小文字無視フラグ
                ignoreCase = !fileNameKeyword.Any(char.IsUpper)
                             && !folderNameKeywords.Any(static k => k.Any(char.IsUpper));

                // 拡張子関連の抽出
                filterWithoutExtension = Path.GetFileNameWithoutExtension(fileNameKeyword);
                filterExtension = Path.GetExtension(fileNameKeyword);
                hasExtension = filterExtension.Length > 0;

                // ベースフォルダで絞り込み
                var targetGuids = data.GetCachedGuidsFilteredByFolder(folderPath);

                // フィルタリング開始（並列）
                var matchedGuids = targetGuids
                    .AsParallel()
                    .Where(guid =>
                    {
                        string assetPath = data.GetAssetPathFromGuid(guid);
                        string dirPart = Path.GetDirectoryName(assetPath)
                            ?.Replace('\\', '/')
                            ?? "";
                        string fileNameWithExt = Path.GetFileName(assetPath);

                        // ◆◆ フォルダ名サブシーケンスマッチ ◆◆
                        //    folderNameKeywords の各トークンごとに dirPart にサブシーケンスマッチするかを確認
                        foreach (var folderKey in folderNameKeywords)
                        {
                            if (!searchResultFilter2.Filter(dirPart, folderKey, ignoreCase))
                            {
                                return false;
                            }
                        }

                        // ◆◆ ファイル名サブシーケンスマッチ ◆◆
                        // 1) 拡張子を除いた実ファイル名部分のサブシーケンスマッチ
                        string justFileName = Path.GetFileNameWithoutExtension(fileNameWithExt);
                        if (!searchResultFilter2.Filter(justFileName, filterWithoutExtension, ignoreCase))
                        {
                            return false;
                        }

                        // 2) 拡張子があれば EndsWith で判定
                        if (hasExtension)
                        {
                            if (!fileNameWithExt.EndsWith(filterExtension,
                                ignoreCase ? StringComparison.OrdinalIgnoreCase
                                           : StringComparison.Ordinal))
                            {
                                return false;
                            }
                        }

                        return true;
                    })
                    .ToList();

                // SearchResult 生成＋priority算出
                var allResults = matchedGuids
                    .AsParallel()
                    .Select(guid =>
                    {
                        var sr = new SearchResult(data, guid, ignoreCase);
                        // Priority計算は最後のキーワード(=ファイル名キーワード)をベースに行うことが多い
                        sr.CalculatePriority(priorityCalculator, fileNameKeyword);
                        return sr;
                    })
                    .ToList();

                // 並べ替え + 上位50件
                totalLength = allResults.Count;
                var top50 = allResults
                    .OrderByDescending(x => x.priority)
                    .Take(50)
                    .ToList();

                filteredResult = top50;
                searchResults = top50;
                return;
            }
            else
            {
                // 他タブ(Hierarchy, TextInHierarchy, MenuCommand, History)は従来の流れ
                List<SearchResult> allResults = new List<SearchResult>();

                // History以外はフィルタが短すぎると結果ゼロに
                if (currentTabName != "History" && leftover.Length < 2)
                {
                    searchResults = Enumerable.Empty<SearchResult>();
                    totalLength = 0;
                    return;
                }

                ignoreCase = !leftover.Any(char.IsUpper);
                filterWithoutExtension = Path.GetFileNameWithoutExtension(leftover);
                filterExtension = Path.GetExtension(leftover);
                hasExtension = filterExtension.Length > 0;

                switch (currentTabName)
                {
                    case "MenuCommand":
                        var menuOnly = data.allMenuCommands
                            .Select(menuPath => SearchResult.CreateCommandResult(menuPath, ignoreCase));
                        menuOnly = Filter(menuOnly);
                        allResults.AddRange(menuOnly);
                        break;

                    case "Hierarchy":
                        var hierarchyOnly = Object.FindObjectsOfType<GameObject>()
                            .Select(go => SearchResult.CreateHierarchyResult(go, ignoreCase));
                        hierarchyOnly = Filter(hierarchyOnly);
                        allResults.AddRange(hierarchyOnly);
                        break;

                    case "TextInHierarchy":
                        var textOnly = Object.FindObjectsOfType<GameObject>()
                            .Select(go => SearchResult.CreateTextInHierarchyResult(go, ignoreCase));
                        textOnly = Filter(textOnly);
                        allResults.AddRange(textOnly);
                        break;

                    case "History":
                        var hist = data.history
                            .Where(x => x != null && FilterSingle(x))
                            .ToList();
                        totalLength = hist.Count;
                        filteredResult = hist.Take(50).ToList();
                        searchResults = filteredResult;
                        return;
                }

                // 並べ替え
                var sorted = allResults.OrderByDescending(x => x.priority);
                searchResults = sorted;

                var list = searchResults.ToList();
                totalLength = list.Count;

                // 先頭50件だけ
                filteredResult = list.Take(50).ToList();
            }
        }

        /// <summary>
        /// (Hierarchy, MenuCommand用) 既存のサブシーケンスフィルタを使ったフィルタ処理
        /// </summary>
        private IEnumerable<SearchResult> Filter(IEnumerable<SearchResult> results)
        {
            var filtered = results
                // サブシーケンスマッチ(filterWithoutExtension)を利用
                .Where(x => searchResultFilter2.Filter(x, filterWithoutExtension))
                // 拡張子があるなら EndsWith 判定
                .Where(x => !hasExtension || x.fileNameWithExt.EndsWith(filterExtension,
                    ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                .Select(x =>
                {
                    x.CalculatePriority(priorityCalculator, filterWithoutExtension);
                    return x;
                });

            return filtered;
        }

        /// <summary>
        /// 単一SearchResultへのフィルタ(History用)
        /// </summary>
        private bool FilterSingle(SearchResult result)
        {
            // Historyタブは length < 2 でもOKにする等、ここはご自由に
            if (data.searchFilter.Length < 2 && data.selectedTab != 4) return true;

            if (!searchResultFilter1.Filter(result, filterWithoutExtension)) return false;
            if (!searchResultFilter2.Filter(result, filterWithoutExtension)) return false;
            if (hasExtension && !result.fileNameWithExt.EndsWith(filterExtension,
                ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            {
                return false;
            }

            return true;
        }

        public void OnFullSearchFilterChanged(string newFullSearchFilter)
        {
            data.fullSearchFilter = newFullSearchFilter;
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = new CancellationTokenSource();
            SearchAsyncWrapper(cancellationTokenSource.Token).Forget();
        }
    }
}
