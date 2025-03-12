using System.IO;
using UnityEditor;
using UnityEngine;

namespace Room6.TSearch.Editor
{
    public class MoveAssetsButton
    {
        private SearchResult result;
        private string dstPath => result.assetPath;

        public MoveAssetsButton(SearchResult result)
        {
            this.result = result;
        }
        
        public static Object GetSelectedObject()
        {
            if (Selection.objects.Length == 0)
            {
                return null;
            }
            
            return Selection.objects[0];
        }
        
        public static string GetMessage(string _dstPath)
        {
            var srcPathList = new System.Text.StringBuilder();
            var totalAssetCount = Selection.objects.Length;

            int loopCount = Mathf.Min(Selection.objects.Length, 10);
            bool isTruncated = Selection.objects.Length > 10;

            for (var i = 0; i < loopCount; i++)
            {
                var path = AssetDatabase.GetAssetPath(Selection.objects[i]);
                string fileName = Path.GetFileName(path);
                srcPathList.Append("\"");
                srcPathList.Append(fileName);
                srcPathList.Append("\"");
                
                if (i < loopCount - 1)
                {
                    srcPathList.Append(", ");
                }
                else
                {
                    srcPathList.Append(" ");
                }
            }

            if (isTruncated)
            {
                srcPathList.Append(", ...");
            }
            
            var dstPathList = new System.Text.StringBuilder();
            dstPathList.Append(_dstPath);
            
            return $"Move {srcPathList}to <color=blue>{dstPathList}</color> (Total: {totalAssetCount} asset(s)) ";
        }

        public void OnGUI(TSearchController controller, TSearchEditorWindow window)
        {
            var pathList = new System.Text.StringBuilder();
            var maxLength = 40;
            var totalAssetCount = Selection.objects.Length;

            pathList.AppendLine("src: ");
            int loopCount = Mathf.Min(Selection.objects.Length, 10);
            bool isTruncated = Selection.objects.Length > 10;

            for (var i = 0; i < loopCount; i++)
            {
                var path = AssetDatabase.GetAssetPath(Selection.objects[i]);
                pathList.Append("・");
                
                string fileName = Path.GetFileName(path);
                pathList.AppendLine(fileName);
            }

            if (isTruncated)
            {
                pathList.AppendLine("...");
            }

            pathList.AppendLine();
            pathList.AppendLine("dst: ");
            pathList.Append(dstPath);
            
            var message = $"Move {totalAssetCount} asset(s) to the following directory?\n\n{pathList}";
            
            GUIContent moveToFolderIcon = EditorGUIUtility.IconContent("d_FolderOpened Icon");
            if (GUILayout.Button(moveToFolderIcon, GUIStyle.none, GUILayout.Width(16), GUILayout.Height(16)))
            {
                if (EditorUtility.DisplayDialog("Confirm", message, "OK", "Cancel"))
                {
                    controller.MoveTo(result, Selection.objects);
                    window.CheckClose();
                }
            }
        }
    }
}