using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        public static readonly string[] TabNames = { "All", "Assets", "Hierarchy", "MenuCommand", "History" };

        public IEnumerable<SearchResult> searchResults;
        public List<SearchResult>        filteredResult = new();
        public int                       totalLength;
        public Priority                  priorityCalculator  = new SimplePriority();
        public SearchFilter              searchResultFilter1 = new SimpleLengthFilter();
        public SearchFilter              searchResultFilter2 = new SubsequenceFilter();
        public SearchResult activeResult { get; protected set; }
        public int activeIndex { get; protected set; } = -1;

        public CancellationTokenSource cancellationTokenSource;

        public TSearchData data => TSearchData.instance;

        // 検索
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

            SearchAsyncWrapper(cancellationTokenSource.Token);
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

            // if (activeResult.resultType == ResultType.Hierarchy)
            {
                EditorGUIUtility.PingObject(activeResult.asset);
            }
        }

        public void ResetActive()
        {
            activeIndex = 0;
            activeResult = null;
        }

        public void ChangeTabNext(int direction)
        {
            ResetActive();
            data.selectedTab = (data.selectedTab + direction + TabNames.Length) % TabNames.Length;

            OnTabChanged();
        }

        public void OnTabChanged()
        {
            var filter = (ResultType)Enum.Parse(typeof(ResultType), TabNames[data.selectedTab]);

            if (searchResults == null)
            {
                filteredResult.Clear();
            }
            else
            {
                if (filter == ResultType.History)
                {
                    totalLength = data.history.Count;
                    filteredResult = data.history.Take(50).ToList();
                }
                else
                {
                    filteredResult = searchResults.Where(result => (filter & result.resultType) != 0).ToList();
                    totalLength = filteredResult.Count;
                    filteredResult = filteredResult.Take(50).ToList();
                }
            }
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

                SearchAsyncWrapper(cancellationTokenSource.Token);
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

        public async Task SearchAsyncWrapper(CancellationToken cancellationToken)
        {
            try
            {
                await SearchAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation, if needed.
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                cancellationTokenSource = null;
            }

            EditorWindow.GetWindow<TSearchEditorWindow>("TSearch").Repaint();
        }

        private async Task SearchAsync(CancellationToken token)
        {
            activeIndex = 0;
            activeResult = null;

            if (data.searchFilter.Length >= 2)
            {
                ignoreCase = !data.searchFilter.Any(char.IsUpper);
                filterWithoutExtension = Path.GetFileNameWithoutExtension(data.searchFilter);
                filterExtension = Path.GetExtension(data.searchFilter);
                hasExtension = filterExtension.Length > 0;
                await Task.Delay(1, cancellationToken: token);

                // メニューコマンドの検索
                var menuCommands = data.allMenuCommands
                    .Select(menuPath => SearchResult.CreateCommandResult(menuPath, ignoreCase));
                menuCommands = Filter(menuCommands);
                await Task.Delay(1, cancellationToken: token);

                // ヒエラルキーの検索
                var hierarchys = await SearchHierarchyAsync(token);
                await Task.Delay(1, cancellationToken: token);

                var results = AssetDatabase.FindAssets("", new[] { "Assets" })
                    .Select(guid => new SearchResult(guid, ignoreCase));
                results = Filter(results);
                await Task.Delay(1, cancellationToken: token);

                var menuCommandsList = menuCommands.ToList();
                var resultList = results.ToArray();
                var hierarchyList = hierarchys.ToArray();
                await Task.Delay(1, cancellationToken: token);

                var allResults = new List<SearchResult>();
                allResults.AddRange(menuCommandsList);
                allResults.AddRange(resultList);
                allResults.AddRange(hierarchyList);
                await Task.Delay(1, cancellationToken: token);

                results = allResults
                    .OrderByDescending(x => x.priority);
                // .Take(50);
                // .Select(x =>
                // {
                //     x.LoadAsset();
                //     return x;
                // })
                // .Where(x => x.asset != null);
                searchResults = results;
            }
            else
            {
                searchResults = null;
            }

            OnTabChanged();
        }

        private async Task<IEnumerable<SearchResult>> SearchHierarchyAsync(CancellationToken token)
        {
            IEnumerable<SearchResult> results;
            if (data.searchFilter.Length >= 2)
            {
                results = Object.FindObjectsOfType<GameObject>()
                    .Select(go => SearchResult.CreateHierarchyResult(go, ignoreCase));
                results = Filter(results);
            }
            else
            {
                results = null;
            }

            return results;
        }

        private IEnumerable<SearchResult> Filter(IEnumerable<SearchResult> results)
        {
            if (data.searchFilter.Length >= 2)
            {
                // メニューコマンドの検索
                results = results.Where(x => searchResultFilter1.Filter(x, filterWithoutExtension));
                results = results.Where(x => searchResultFilter2.Filter(x, filterWithoutExtension));
                results = results.Where(x => !hasExtension || x.fileNameWithExt.EndsWith(filterExtension));
                results = results.Select(x =>
                {
                    x.CalculatePriority(priorityCalculator, filterWithoutExtension);
                    return x;
                });
            }

            return results;
        }
    }
}