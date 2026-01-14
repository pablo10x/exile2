//Purpose of this script is to remove unused and deprecated files from the previous versions.
//After the update is finished, this script is self-destructed.


using System.IO;

using UnityEngine;
using UnityEditor;


namespace AmazingAssets.TerrainToMesh.Editor
{
    static public class Updater
    {
        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            string thisAssetPath = GetThisAssetProjectPath();

            //Remove 'Documentation' folder
            string directoryPath = Path.Combine(thisAssetPath, "Documentation");
            if (Directory.Exists(directoryPath))
                AssetDatabase.DeleteAsset(directoryPath);

            //Remove 'Manual' folder
            directoryPath = Path.Combine(thisAssetPath, "Editor", "Manual");
            if (Directory.Exists(directoryPath))
                AssetDatabase.DeleteAsset(directoryPath);

            //Remove 'png' icons
            directoryPath = Path.Combine(thisAssetPath, "Editor", "Icons");
            if (Directory.Exists(directoryPath))
            {
                string[] icons = Directory.GetFiles(directoryPath, "*.png");
                for (int i = 0; i < icons.Length; i++)
                {
                    AssetDatabase.DeleteAsset(icons[i]);
                }
            }

         
        }

        static internal string GetThisAssetProjectPath()
        {
            string fileName = "AmazingAssets.TerrainToMeshEditor";

            string[] assets = AssetDatabase.FindAssets(fileName, null);
            if (assets != null && assets.Length > 0)
            {
                string currentFilePath = AssetDatabase.GUIDToAssetPath(assets[0]);
                return Path.GetDirectoryName(Path.GetDirectoryName(currentFilePath));
            }
            else
            {
                return string.Empty;
            }
        }
    }
}