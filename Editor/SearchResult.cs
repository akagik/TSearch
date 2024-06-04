using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Room6.TSearch.Editor
{
    [System.Serializable]
    public class SearchResult
    {
        private const int MaxDisplayFolderLength = 30;

        public readonly ResultType resultType;
        public readonly string assetPath;
        public readonly string guid;

        public int    priority;
        public string fileName;
        public string fileNameWithExt;
        public Object asset;
        public bool   ignoreCase;

        public string parentDirPath;
        public string shortenParentDirPath;

        public bool IsDirectory => resultType == ResultType.Assets && Directory.Exists(assetPath);
        
        private static Dictionary<Type, Func<object, string>> type2TextStringMethodCache = new();

        private static BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        public static SearchResult CreateCommandResult(string command, bool ignoreCase)
        {
            SearchResult result = new SearchResult(command, command, ResultType.MenuCommand);
            result.ignoreCase = ignoreCase;
            result.fileName = command;
            result.fileNameWithExt = command;
            return result;
        }

        public static SearchResult CreateHierarchyResult(GameObject go, bool ignoreCase)
        {
            SearchResult result = new SearchResult(go.name, go.GetInstanceID().ToString(), ResultType.Hierarchy);
            result.ignoreCase = ignoreCase;
            result.fileName = go.name;
            result.fileNameWithExt = go.name;
            result.asset = go;
            return result;
        }

        public static SearchResult CreateTextInHierarchyResult(GameObject go, bool ignoreCase)
        {
            SearchResult result = new SearchResult(go.name, go.GetInstanceID().ToString(), ResultType.TextInHierarchy);
            result.ignoreCase = ignoreCase;
            result.asset = go;

            StringBuilder sb = new();
            foreach (var component in go.GetComponents<Component>())
            {
                if (component == null)
                {
                    continue;
                }
                var type = component.GetType();
                if (!type2TextStringMethodCache.ContainsKey(type))
                {
                    if (type.GetField("m_Text", Flags) is { } f1)
                    {
                        type2TextStringMethodCache[type] = obj => f1.GetValue(obj) as string;
                    }
                    else if (type.GetField("m_text", Flags) is { } f2)
                    {
                        type2TextStringMethodCache[type] = obj => f2.GetValue(obj) as string;
                    }
                    else
                    {
                        type2TextStringMethodCache[type] = null;
                    }
                }

                if (type2TextStringMethodCache[type] == null) continue;
            
                if (type2TextStringMethodCache[type].Invoke(component) is { } value)
                {
                    sb.Append(value);
                    sb.Append(",");
                }
            }

            string text = sb.ToString();
            result.fileName = text;
            result.fileNameWithExt = text;

            return result;
        }

        public SearchResult(string assetPath, string guid, ResultType resultType)
        {
            this.assetPath = assetPath;
            this.guid = guid;
            this.resultType = resultType;
            
            priority = -1;
        }

        public SearchResult(string guid, bool ignoreCase)
        {
            this.resultType = ResultType.Assets;
            this.guid = guid;
            this.ignoreCase = ignoreCase;

            assetPath = AssetDatabase.GUIDToAssetPath(guid);
            fileNameWithExt = Path.GetFileName(assetPath);
            fileName = Path.GetFileNameWithoutExtension(assetPath);
            priority = -1;
        }

        public Object LoadAsset()
        {
            if (asset is not null || resultType == ResultType.MenuCommand)
            {
                return null;
            }

            asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);

            parentDirPath = Path.GetDirectoryName(assetPath);

            // Remove "Assets" from the parentFolderPath and truncate if too long
            if (parentDirPath != null && parentDirPath.StartsWith("Assets"))
            {
                shortenParentDirPath = parentDirPath.Substring(6); // この行を追加
                if (shortenParentDirPath.Length > MaxDisplayFolderLength) // この行を追加
                {
                    shortenParentDirPath =
                        "..." + shortenParentDirPath.Substring(shortenParentDirPath.Length -
                                                               MaxDisplayFolderLength); // この行を追加
                }
            }
            else
            {
                shortenParentDirPath = parentDirPath;
            }

            return asset;
        }

        public void CalculatePriority(Priority priorityCalculator, string searchFilter)
        {
            priority = priorityCalculator.GetPriority(this, searchFilter);
        }

        public void Execute()
        {
            if (resultType == ResultType.Assets)
            {
                AssetDatabase.OpenAsset(asset);
            }
            else if (resultType == ResultType.MenuCommand)
            {
                EditorApplication.ExecuteMenuItem(assetPath);
            }
            else if (resultType is ResultType.Hierarchy or ResultType.TextInHierarchy )
            {
                EditorGUIUtility.PingObject(asset);
                Selection.activeGameObject = (GameObject)asset;
            }
        }

        // Equality Methods
        protected bool Equals(SearchResult other)
        {
            return resultType == other.resultType && assetPath == other.assetPath && guid == other.guid;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SearchResult)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int)resultType, assetPath, guid);
        }
    }
}