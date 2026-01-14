namespace FewClicksDev.Core
{
    using UnityEditor;
    using UnityEngine.Events;

    public class AssetsPostprocessor : AssetPostprocessor
    {
        public static event UnityAction OnAssetsImported = null;

        public static void OnPostprocessAllAssets(string[] _importedAssets, string[] _deletedAssets, string[] _movedAssets, string[] _movedFromAssetPaths, bool _didDomainReload)
        {
            OnAssetsImported?.Invoke();
        }
    }
}
