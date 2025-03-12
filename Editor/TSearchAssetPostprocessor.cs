namespace Room6.TSearch.Editor
{
    using UnityEditor;

    public class TSearchAssetPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            // 何らかのアセットが変更された場合はキャッシュをリセットする
            if (importedAssets.Length > 0 || deletedAssets.Length > 0 || movedAssets.Length > 0)
            {
                TSearchData.instance.ResetAssetCache();
            }
        }
    }
}