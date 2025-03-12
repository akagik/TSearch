using UnityEditor;
using Room6.TSearch.Editor;

public class TSearchAssetPostprocessor : AssetPostprocessor
{
    static void OnPostprocessAllAssets(
        string[] importedAssets, 
        string[] deletedAssets, 
        string[] movedAssets, 
        string[] movedFromAssetPaths)
    {
        // 変更があった場合、個別にキャッシュを更新する
        if (importedAssets.Length > 0 || deletedAssets.Length > 0 || movedAssets.Length > 0)
        {
            TSearchData.instance.UpdateAssetCache(importedAssets, deletedAssets, movedAssets);
        }
    }
}