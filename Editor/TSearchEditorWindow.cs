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
    public class TSearchEditorWindow : EditorWindow
    {
        public const float RowHeight = 20;
        public const float SearchStartY = 83f;

        private TSearchController controller = new();
        private Vector2 scrollPosition;
        private GUIStyle searchFieldStyle;

        [MenuItem("Window/TSearch %T")]
        public static void ShowWindow()
        {
            GetWindow<TSearchEditorWindow>("TSearch");
        }

        private void OnEnable()
        {
            controller.OnEnable();
        }

        private void OnGUI()
        {
            if (!docked)
            {
                if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
                {
                    this.Close();
                }
            }

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return &&
                controller.activeIndex >= 0)
            {
                if (0 <= controller.activeIndex &&
                    controller.activeIndex < controller.filteredResult.Count)
                {
                    var result = controller.filteredResult[controller.activeIndex];
                    bool alt = Event.current.alt;

                    if (alt)
                    {
                        controller.JumpToAsset(result);
                    }
                    else
                    {
                        controller.Execute(result);
                        CheckClose();
                    }
                }
            }

            // Handle up and down arrow keys for selecting active result
            if (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.DownArrow ||
                                                            Event.current.keyCode == KeyCode.UpArrow))
            {
                controller.OnActiveMoved(Event.current.keyCode == KeyCode.UpArrow);
                Event.current.Use();
            }

            DrawSearchField();
            GUILayout.Space(5);

            // if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
            // {
            //     GUI.FocusControl(null);
            // }

            // Detect Tab key press and cycle through tabs
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Tab)
            {
                scrollPosition = new Vector2(scrollPosition.x, 0);
                int direction = Event.current.shift ? -1 : 1;
                controller.ChangeTabNext(direction);
                GUI.FocusControl(null);
                Event.current.Use();
            }

            GUILayout.Space(5);
            DrawTabs();
            GUILayout.Space(5);
            DrawSearchResults();
        }

        private void DrawSearchField()
        {
            if (searchFieldStyle == null)
            {
                searchFieldStyle = new GUIStyle("SearchTextField");
                searchFieldStyle.fixedHeight = 20;
            }

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);

            GUI.SetNextControlName("SearchField");
            EditorGUI.FocusTextInControl("SearchField");

            // ここでは TSearchData.instance.fullSearchFilter を直接編集
            string newFullSearchFilter = EditorGUILayout.TextField(
                TSearchData.instance.fullSearchFilter,
                searchFieldStyle,
                GUILayout.ExpandWidth(true)
            );

            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                controller.ClearSearch();
                newFullSearchFilter = ""; // クリア
            }

            GUILayout.EndHorizontal();

            // 変更があればコントローラに通知
            if (newFullSearchFilter != TSearchData.instance.fullSearchFilter)
            {
                TSearchData.instance.fullSearchFilter = newFullSearchFilter;
                controller.OnFullSearchFilterChanged(newFullSearchFilter);
            }
        }

        // 検索タイプの切り替え用タブを描画
        private void DrawTabs()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            controller.data.selectedTab = GUILayout.Toolbar(controller.data.selectedTab, TSearchController.TabNames,
                GUILayout.ExpandWidth(true));
            GUILayout.Space(10);
            GUILayout.EndHorizontal();
        }

        private void DrawSearchResults()
        {
            GUILayout.Label("Search Results: " + controller.totalLength);

            if (controller.activeResult != null)
            {
                float activeY = controller.activeIndex * RowHeight;
                if (activeY - scrollPosition.y + RowHeight > position.height - SearchStartY)
                {
                    float y = activeY - position.height + SearchStartY + RowHeight;
                    scrollPosition = new Vector2(scrollPosition.x, y);
                }
                else if (activeY - scrollPosition.y < 0)
                {
                    float y = activeY;
                    scrollPosition = new Vector2(scrollPosition.x, y);
                }
            }

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            for (var i = 0; i < controller.filteredResult.Count; i++)
            {
                var result = controller.filteredResult[i];
                DrawSearchResult(result, i);
            }

            GUILayout.EndScrollView();
        }

        private void DrawSearchResult(SearchResult result, int index)
        {
            result.LoadAsset();

            Texture icon = TSearchUtils.GetIconForType(result);
            GUIContent linkIcon = EditorGUIUtility.IconContent("d_Linked");

            GUILayout.BeginHorizontal();

            GUILayout.Label(icon, GUILayout.Width(16), GUILayout.Height(16));

            var style = new GUIStyle(GUI.skin.label)
            {
                fixedHeight = RowHeight
            };

            if (index == controller.activeIndex)
            {
                style.normal.background = Texture2D.grayTexture;
                GUI.skin.settings.selectionColor = new Color(0.2f, 0.6f, 1f, 1f);
            }

            if (GUILayout.Button($"{result.fileNameWithExt} ({result.priority})", style))
            {
                controller.Execute(result);
                CheckClose();
            }

            if (result.resultType == ResultType.Assets)
            {
                GUIStyle rightAlignedLabel = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleRight
                };

                if (GUILayout.Button(result.shortenParentDirPath, rightAlignedLabel, GUILayout.Width(240)))
                {
                    UnityEngine.Object parentFolder =
                        AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(result.parentDirPath);
                    AssetDatabase.OpenAsset(parentFolder);
                }

                if (result.IsDirectory)
                {
                    MoveAssetsButton moveBtn = new MoveAssetsButton(result);
                    moveBtn.OnGUI(controller);
                }
            }

            // NOTE: これ置かないと高さがずれる
            var styleButton = new GUIStyle(GUIStyle.none)
            {
                fixedWidth = 16,
                fixedHeight = RowHeight,
            };

            // Show link icon for selection
            if (GUILayout.Button(linkIcon.image, styleButton))
            {
                controller.JumpToAsset(result);
            }

            GUILayout.EndHorizontal();
        }

        private void CheckClose()
        {
            if (!docked)
            {
                this.Close();
            }
        }
    }

    [System.Serializable]
    public class TSearchController
    {
        public static readonly string[] TabNames =
            { "Assets", "Hierarchy", "TextInHierarchy", "MenuCommand", "History" };

        public IEnumerable<SearchResult> searchResults; // 全検索結果
        public List<SearchResult> filteredResult = new(); // 表示用にフィルタされた結果
        public int totalLength;
        public Priority priorityCalculator = new SimplePriority();
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
            (string folderPath, string keyword) = data.ParseFullSearchFilter();

            // ここで、TSearchData.instance.searchFilter にキーワードのみを保存してもOK
            data.searchFilter = keyword;

            // フィルタ用の文字列が短すぎる場合 (必要に応じて制限)
            if (keyword.Length < 2 && currentTabName != "History")
            {
                searchResults = Enumerable.Empty<SearchResult>();
                totalLength = 0;
                return;
            }

            ignoreCase = !keyword.Any(char.IsUpper);
            filterWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(keyword);
            filterExtension = System.IO.Path.GetExtension(keyword);
            hasExtension = filterExtension.Length > 0;

            if (currentTabName == "Assets")
            {
                // 1. キャッシュ済み GUID リストを取得
                var targetGuids = data.GetCachedGuidsFilteredByFolder(folderPath);

                // 2. アセット名の部分一致チェックを並列化
                var matchedGuids = targetGuids
                    .AsParallel()
                    .Where(guid =>
                    {
                        string path = data.GetAssetPathFromGuid(guid);
                        string fileNameWithExt = Path.GetFileName(path);
                        string fileNameWithoutExt = Path.GetFileNameWithoutExtension(path);

                        bool passSubsequence = searchResultFilter2.Filter(
                            fileNameWithoutExt,
                            filterWithoutExtension,
                            ignoreCase
                        );

                        bool passExtension = !hasExtension ||
                                             fileNameWithExt.EndsWith(
                                                 filterExtension,
                                                 ignoreCase
                                                     ? StringComparison.OrdinalIgnoreCase
                                                     : StringComparison.Ordinal
                                             );

                        return passSubsequence && passExtension;
                    })
                    .ToList();

                // 3. SearchResult の生成 ＋ priority 計算も並列化
                //   ※ SearchResult や CalculatePriority の内部で UnityEditor API を呼んでいないことが前提
                var allResults = matchedGuids
                    .AsParallel()
                    .Select(guid =>
                    {
                        var sr = new SearchResult(data, guid, ignoreCase);
                        sr.CalculatePriority(priorityCalculator, filterWithoutExtension);
                        return sr;
                    })
                    .ToList();

                // 4. 並べ替え & 上位 50 件を抽出 (必要であればここも PLINQ で行ってもOK)
                totalLength = allResults.Count(); // 全件数を計算しておく
                var top50 = allResults
                    .AsParallel()
                    .OrderByDescending(static x => x.priority)
                    .Take(50)
                    .ToList();

                totalLength = allResults.Count(); // 全件数を計算しておく
                filteredResult = top50;
                searchResults = top50; 
                return;
            }
            else
            {
                List<SearchResult> allResults = new List<SearchResult>();

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

                // 検索結果を優先度順に並べる
                var sorted = allResults.OrderByDescending(x => x.priority);
                searchResults = sorted;

                var list = searchResults.ToList();
                totalLength = list.Count;

                // 先頭50件のみ表示
                filteredResult = list.Take(50).ToList();
            }
        }

        /// <summary>
        /// 検索結果フィルタ
        /// </summary>
        private IEnumerable<SearchResult> Filter(IEnumerable<SearchResult> results)
        {
            var filtered = results
                .Where(x => searchResultFilter2.Filter(x, filterWithoutExtension))
                .Where(x => !hasExtension || x.fileNameWithExt.EndsWith(filterExtension))
                .Select(x =>
                {
                    x.CalculatePriority(priorityCalculator, filterWithoutExtension);
                    return x;
                });

            return filtered;
        }

        /// <summary>
        /// 単一の SearchResult に対するフィルタ(History 用など)
        /// </summary>
        private bool FilterSingle(SearchResult result)
        {
            if (data.searchFilter.Length < 2) return true; // フィルタしない

            if (!searchResultFilter1.Filter(result, filterWithoutExtension)) return false;
            if (!searchResultFilter2.Filter(result, filterWithoutExtension)) return false;
            if (hasExtension && !result.fileNameWithExt.EndsWith(filterExtension)) return false;

            return true;
        }

        public void OnFullSearchFilterChanged(string newFullSearchFilter)
        {
            // ここで新たな fullSearchFilter を受け取り、必要があればキャンセルして再検索
            data.fullSearchFilter = newFullSearchFilter;
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = new CancellationTokenSource();
            SearchAsyncWrapper(cancellationTokenSource.Token).Forget();
        }
    }
}