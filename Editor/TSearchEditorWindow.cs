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
            // ウィンドウがドッキングしていないなら、Esc押下で閉じる処理
            if (!docked)
            {
                if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
                {
                    // 即座に Close() を呼ばず、次のフレームに延期する
                    EditorApplication.delayCall += Close;
                    Event.current.Use();
                    return;
                }
            }

// Enter押下 (Return押下) に対する処理
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return &&
                controller.activeIndex >= 0)
            {
                // activeIndex の範囲チェック
                if (0 <= controller.activeIndex && controller.activeIndex < controller.filteredResult.Count)
                {
                    var result = controller.filteredResult[controller.activeIndex];
                    // alt 押下判定
                    bool alt = Event.current.alt;
                    // 追加: shift 押下判定
                    bool shift = Event.current.shift;

                    if (shift)
                    {
                        // ◆ Shift + Enter の場合
                        // ファイルを開かず History に追加だけする
                        controller.JumpToAsset(result);
                        // 選択状態にする
                        Selection.activeObject = result.asset;
                        controller.AddHistory(result);
                        CheckClose();
                    }
                    else if (alt)
                    {
                        // ◆ Alt + Enter の場合
                        // JumpToAsset(ただの Ping) だけ行う
                        controller.JumpToAsset(result);
                    }
                    else
                    {
                        // ◆ 通常の Enter の場合
                        // 実行 + ウィンドウを閉じる
                        controller.Execute(result);
                        CheckClose();
                    }
                }
            }

            // ↑↓キー操作
            if (Event.current.type == EventType.KeyDown &&
               (Event.current.keyCode == KeyCode.DownArrow || Event.current.keyCode == KeyCode.UpArrow))
            {
                controller.OnActiveMoved(Event.current.keyCode == KeyCode.UpArrow);
                Event.current.Use();
            }
            
            // --- ここで Ctrl+M のキー入力を検出してフォルダ移動を実行 ---
            if (Event.current.type == EventType.KeyDown &&
                Event.current.control &&
                Event.current.keyCode == KeyCode.M)
            {
                if (controller.activeIndex >= 0)
                {
                    var result = controller.filteredResult[controller.activeIndex];
                    // 対象がフォルダの場合のみ
                    if (result.IsDirectory)
                    {
                        Debug.Log("Ctrl+M: " + MoveAssetsButton.GetMessage(result.parentDirPath));
                        // プロジェクトウィンドウで選択中のオブジェクトをそのフォルダへ移動
                        controller.MoveTo(result, Selection.objects);
                        // 移動後、対象フォルダを Ping する
                        EditorGUIUtility.PingObject(MoveAssetsButton.GetSelectedObject());
                        Event.current.Use();
                        
                        CheckClose();
                    }
                }
            }
            
            DrawSearchField();
            GUILayout.Space(5);

            // タブ切替 (Tabキー押下)
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
            controller.data.selectedTab = GUILayout.Toolbar(
                controller.data.selectedTab,
                TSearchController.TabNames,
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
                    moveBtn.OnGUI(controller, this);
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

        public void CheckClose()
        {
            if (!docked)
            {
                // ウィンドウを即座に閉じず、次のフレームに延期する
                EditorApplication.delayCall += Close;
            }
        }
    }
}
