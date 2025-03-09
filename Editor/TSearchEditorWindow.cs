using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Room6.TSearch.Editor
{
    public class TSearchEditorWindow : EditorWindow
    {
        public const float RowHeight    = 20;
        public const float SearchStartY = 83f;

        private TSearchController controller = new();
        private Vector2           scrollPosition;
        private Vector2           scrollPositionHistory;
        private Rect              scrollViewRect;

        private GUIStyle searchFieldStyle;
        private GUIStyle searchResultStyle;

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

            string newSearchFilter = EditorGUILayout.TextField("", controller.data.searchFilter, searchFieldStyle,
                GUILayout.ExpandWidth(true));


            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                controller.ClearSearch();
            }

            GUILayout.EndHorizontal();

            controller.OnSearchChanged(newSearchFilter);
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

                // if (activeY - scrollPosition.y + RowHeight > position.height - SearchStartY)
                // {
                //     float y = activeY - position.height + SearchStartY + RowHeight;
                //     scrollPosition = new Vector2(scrollPosition.x, y);
                // }
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

            // NOTE: これ置かないと高さがずれる.
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
            // { "All", "Assets", "Hierarchy", "TextInHierarchy", "MenuCommand", "History" };
            { "Assets", "Hierarchy", "TextInHierarchy", "MenuCommand", "History" };

        public IEnumerable<SearchResult> searchResults;     // 全検索結果
        public List<SearchResult>        filteredResult = new(); // 表示用にフィルタされた結果
        public int                       totalLength;
        public Priority                  priorityCalculator  = new SimplePriority();
        public SearchFilter              searchResultFilter1 = new SimpleLengthFilter();
        public SearchFilter              searchResultFilter2 = new SubsequenceFilter();
        public SearchResult activeResult { get; protected set; }
        public int activeIndex { get; protected set; } = -1;

        public CancellationTokenSource cancellationTokenSource;

        public TSearchData data => TSearchData.instance;

        // 検索用
        bool   ignoreCase;
        string filterWithoutExtension;
        string filterExtension;
        bool   hasExtension;

        public void OnEnable()
        {
            data.CacheAllMenuCommands();

            if (cancellationTokenSource == null)
            {
                cancellationTokenSource = new CancellationTokenSource();
            }

            // ウィンドウを開いたとき、あるいはエディタ再起動時に再検索したい場合は呼ぶ
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
            // タブが変わったので検索をやりなおす
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

                if (string.IsNullOrEmpty(selectedAssetPath))
                {
                    continue;
                }

                string selectedFolderPath = Path.GetDirectoryName(selectedAssetPath);

                if (selectedFolderPath == folderPath)
                {
                    continue;
                }

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
        /// 選択タブに応じて必要な検索を行う
        /// </summary>
        private async UniTask SearchAsync(CancellationToken token)
        {
            activeIndex = 0;
            activeResult = null;
            filteredResult.Clear();

            string currentTabName = TabNames[data.selectedTab];

            // フィルタ用の文字列が短すぎる場合は検索しない (2文字未満など)
            if (data.searchFilter.Length < 2 && currentTabName != "History")
            {
                // ヒストリ以外のタブで検索ワードが短すぎる場合は何も表示しない
                searchResults = Enumerable.Empty<SearchResult>();
                totalLength = 0;
                return;
            }

            ignoreCase          = !data.searchFilter.Any(char.IsUpper);
            filterWithoutExtension = Path.GetFileNameWithoutExtension(data.searchFilter);
            filterExtension     = Path.GetExtension(data.searchFilter);
            hasExtension        = filterExtension.Length > 0;

            // タブ別に検索結果を構築する
            List<SearchResult> allResults = new List<SearchResult>();

            switch (currentTabName)
            {
                case "All":
                    // すべてを検索
                    // 1) MenuCommands
                    var menuCommands = data.allMenuCommands
                        .Select(menuPath => SearchResult.CreateCommandResult(menuPath, ignoreCase));
                    menuCommands = Filter(menuCommands);
                    allResults.AddRange(menuCommands);
                    await UniTask.Yield(token);

                    // 2) Hierarchy
                    var hierarchies = Object.FindObjectsOfType<GameObject>()
                        .Select(go => SearchResult.CreateHierarchyResult(go, ignoreCase));
                    hierarchies = Filter(hierarchies);
                    allResults.AddRange(hierarchies);
                    await UniTask.Yield(token);

                    // 3) TextInHierarchy
                    var textInHierarchies = Object.FindObjectsOfType<GameObject>()
                        .Select(go => SearchResult.CreateTextInHierarchyResult(go, ignoreCase));
                    textInHierarchies = Filter(textInHierarchies);
                    allResults.AddRange(textInHierarchies);
                    await UniTask.Yield(token);

                    // 4) Assets
                    var assets = AssetDatabase.FindAssets("", new[] { "Assets" })
                        .Select(guid => new SearchResult(guid, ignoreCase));
                    assets = Filter(assets);
                    allResults.AddRange(assets);
                    break;

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

                case "Assets":
                    var assetsOnly = AssetDatabase.FindAssets("", new[] { "Assets" })
                        .Select(guid => new SearchResult(guid, ignoreCase));
                    assetsOnly = Filter(assetsOnly);
                    allResults.AddRange(assetsOnly);
                    break;

                case "History":
                    // ヒストリをそのまま表示 (フィルタは不要なら省略も可)
                    // 今回は例として、ヒストリもフィルタする場合は下記のように
                    // フィルタしてもいいし、しなくても良い
                    var hist = data.history
                        .Where(x => x != null && FilterSingle(x))
                        .ToList();
                    totalLength = hist.Count;
                    // 50件に制限するとき
                    filteredResult = hist.Take(50).ToList();
                    searchResults = filteredResult;
                    return;
            }

            // 検索結果を優先度順に並べる
            var sorted = allResults
                .OrderByDescending(x => x.priority);

            // まとめて確定
            searchResults = sorted;

            // 表示用フィルタリング
            var list = searchResults.ToList();
            totalLength = list.Count;

            // とりあえず先頭 50 件を表示
            filteredResult = list.Take(50).ToList();
        }

        /// <summary>
        /// 検索結果フィルタ
        /// </summary>
        private IEnumerable<SearchResult> Filter(IEnumerable<SearchResult> results)
        {
            // 必要に応じて検索条件を加える
            // ここでは検索ワード (filterWithoutExtension) が含まれるかチェックする例
            // かつ hasExtension が true の場合は拡張子マッチも行う

            var filtered = results
                .Where(x => searchResultFilter1.Filter(x, filterWithoutExtension))
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
    }
}
