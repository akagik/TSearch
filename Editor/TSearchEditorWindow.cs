using UnityEditor;
using UnityEngine;

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

                if (GUILayout.Button(result.shortenParentDirPath, rightAlignedLabel, GUILayout.Width(340)))
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
}