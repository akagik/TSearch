using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Room6.TSearch.Editor
{
    public static class TSearchUtils
    {
        public static Texture GetIconForType(SearchResult result)
        {
            if (result.resultType == ResultType.MenuCommand)
            {
                GUIContent linkIcon = EditorGUIUtility.IconContent("d__Popup");
                return linkIcon.image;
            }

            if (result.resultType == ResultType.Hierarchy)
            {
                return EditorGUIUtility.ObjectContent(null, typeof(GameObject)).image;
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
                return EditorGUIUtility.ObjectContent(null, typeof(Texture2D)).image;
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

        public static List<string> GetAllMenuCommands()
        {
            List<string> outNames = new();
            List<string> outShortcuts = new();
            MenuHelper.GetMenuItemDefaultShortcuts(outNames, outShortcuts);

            return outNames;
        }

        /// <summary>
        /// 現在選択しているフォルダのパスを返す。
        /// もしフォルダが一つも選択されていなければ "Assets" を返す。
        /// </summary>
        /// <returns>フォルダのパス配列</returns>
        public static string GetSelectedFolderPathOrDefault()
        {
            // 選択中のオブジェクトの中から "DefaultAsset" (プロジェクト上のファイル/フォルダ) を絞り込む
            var selectedObjects = Selection.GetFiltered<DefaultAsset>(SelectionMode.Assets);

            var folderPaths = new List<string>();
            foreach (var obj in selectedObjects)
            {
                // 選択したオブジェクトのパスを取得
                var path = AssetDatabase.GetAssetPath(obj);

                // フォルダかどうかをチェック
                if (AssetDatabase.IsValidFolder(path))
                {
                    folderPaths.Add(path);
                }
            }

            // 一つもフォルダが見つからなければ "Assets" を返す
            if (folderPaths.Count == 0)
            {
                folderPaths.Add("Assets");
            }

            return folderPaths[0];
        }
    }
}