using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Room6.TSearch.Editor
{
    public static class TSearchUtils
    {
        public static Texture GetIconForType(SearchResult result)
        {
            if (result == null)
            {
                Debug.LogError("result is null");
                return null;
            }
            
            if (result.resultType == ResultType.MenuCommand)
            {
                GUIContent linkIcon = EditorGUIUtility.IconContent("d__Popup");
                return linkIcon.image;
            }

            if (result.resultType == ResultType.Hierarchy)
            {
                return EditorGUIUtility.ObjectContent(null, typeof(GameObject)).image;
            }

            // asset が null の場合は処理を中断
            if (result.asset == null)
            {
                Debug.LogError("asset is null");
                return null;
            }

            System.Type type = result.asset.GetType();

            if (type == typeof(GameObject))
            {
                return EditorGUIUtility.ObjectContent(null, typeof(GameObject)).image;
            }
            else if (type == typeof(Texture2D))
            {
                var texture = result.asset as Texture2D;
                return texture;
            }
            else if (type == typeof(AudioClip))
            {
                return EditorGUIUtility.ObjectContent(null, typeof(AudioClip)).image;
            }
            else if (type == typeof(SceneAsset))
            {
                return EditorGUIUtility.ObjectContent(null, typeof(SceneAsset)).image;
            }
            else if (type == typeof(MonoScript))
            {
                return EditorGUIUtility.ObjectContent(null, typeof(MonoScript)).image;
            }
            else if (AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(result.asset)))
            {
                return EditorGUIUtility.IconContent("Folder Icon").image;
            }

            return null;
        }

        public static bool AreCharsEqualIgnoreCase(char str1, char str2)
        {
            // i 文字目を取得し、大文字小文字を無視して比較
            return char.ToUpperInvariant(str1) == char.ToUpperInvariant(str2);
        }

        /// <summary>
        /// 現在選択しているフォルダのパスを返す。フォルダがなければ "Assets"。
        /// 戻り値はバックスラッシュエスケープ付き文字列になります。
        /// 例: "Assets/My Folder" -> "Assets/My\ Folder"
        ///     (さらに "\ " の前の '\' をエスケープして、最終的に "Assets/My\\ Folder" とする場合は
        ///      下記のメソッド実装に合わせてご調整ください)
        /// </summary>
        public static string GetSelectedFolderPathOrDefault()
        {
            // 選択中のフォルダをすべて取得
            var selectedObjects = Selection.GetFiltered<DefaultAsset>(SelectionMode.Assets);
            var folderPaths = new List<string>();
            foreach (var obj in selectedObjects)
            {
                var path = AssetDatabase.GetAssetPath(obj);
                if (AssetDatabase.IsValidFolder(path))
                {
                    folderPaths.Add(path);
                }
            }

            // 一つもフォルダが見つからなければ "Assets"
            if (folderPaths.Count == 0)
            {
                return "Assets";
            }

            // 先頭のフォルダをエスケープして返す
            return EscapeFolderPathForInDirective(folderPaths[0]);
        }

        /// <summary>
        /// in:ディレクティブ向けに、フォルダパス中の「半角スペース」や「バックスラッシュ」をエスケープ。
        /// 例: 
        ///   "Assets/My Folder" -> "Assets/My\ Folder"
        ///   "Assets\\Test Folder" -> "Assets\\\ Test\ Folder" (パターン次第)
        ///   
        /// ※本コードでは、シンプルに
        ///   ' ' => '\ '
        ///   '\' => '\\'
        ///   を行っています。
        ///   
        /// Parse側(Ex: ParseFullSearchFilterWithEscape)が "バックスラッシュ+文字" を
        /// アンエスケープする仕様になっている前提です。
        /// </summary>
        private static string EscapeFolderPathForInDirective(string folderPath)
        {
            var sb = new StringBuilder();
            foreach (char c in folderPath)
            {
                if (c == '\\')
                {
                    // バックスラッシュ自体を二重化
                    sb.Append("\\\\");
                }
                else if (c == ' ')
                {
                    // スペースを "\ " に
                    sb.Append("\\ ");
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
        
        public static List<string> GetAllMenuCommands()
        {
            List<string> outNames = new();
            List<string> outShortcuts = new();
            MenuHelper.GetMenuItemDefaultShortcuts(outNames, outShortcuts);

            return outNames;
        }
    }
}
