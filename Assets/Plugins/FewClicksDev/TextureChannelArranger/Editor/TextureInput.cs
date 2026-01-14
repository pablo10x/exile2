namespace FewClicksDev.TextureChannelArranger
{
    using FewClicksDev.Core;
    using FewClicksDev.Core.ReorderableList;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Rendering;

    [System.Serializable]
    public class TextureInput : TextureReference
    {
        public const string DEFAULT_NAME = "Input_1";

        [SerializeField] private TextureAddMode addMode = TextureAddMode.Search;
        [SerializeField] private string folderPath = string.Empty;
        [SerializeField] private string nameFilter = string.Empty;
        [SerializeField] private string prefix = string.Empty;
        [SerializeField] private string suffix = string.Empty;
        [SerializeField] private List<Texture2D> foundTextures = new List<Texture2D>();

        private int numberOfChannels = 0;

        public override int NumberOfChannels => numberOfChannels;

        public int NumberOfFoundTextures => foundTextures.Count;
        public Texture2DReorderableList TexturesList = null;

        public string FolderPath
        {
            get => folderPath;
            set => folderPath = value;
        }

        public TextureAddMode AddMode
        {
            get => addMode;
            set => addMode = value;
        }

        public string NameFilter
        {
            get => nameFilter;
            set => nameFilter = value;
        }

        public string Prefix
        {
            get => prefix;
            set => prefix = value;
        }

        public string Suffix
        {
            get => suffix;
            set => suffix = value;
        }

        public TextureInput()
        {
            textureName = "Input";
        }

        public TextureInput(string _name)
        {
            textureName = _name;
        }

        public TextureInput(TextureInput _template)
        {
            textureName = _template.TextureName;
            description = _template.Description;
            reference = _template.Reference;

            numberOfChannels = _template.NumberOfChannels;
            prefix = _template.Prefix;
            suffix = _template.Suffix;
            folderPath = _template.FolderPath;
        }

        public TextureInput GetCopy()
        {
            return new TextureInput(this);
        }

        public void SetTexture(Texture2D _texture)
        {
            reference = _texture;
            RecalculateNumberOfChannels();
        }

        public void RecalculateNumberOfChannels()
        {
            if (reference == null)
            {
                numberOfChannels = 0;
                return;
            }

            string _path = AssetDatabase.GetAssetPath(reference);

            TextureImporter _importer = TextureImporter.GetAtPath(_path) as TextureImporter;
            TextureImporterType _type = _importer.textureType;

            if (_type is TextureImporterType.SingleChannel)
            {
                numberOfChannels = 1;
                return;
            }

            previewMask = ColorWriteMask.All;
            isNormal = _importer.textureType is TextureImporterType.NormalMap;
            isSRGB = _importer.sRGBTexture;
            numberOfChannels = _importer.DoesSourceTextureHaveAlpha() ? 4 : 3;
        }

        public void FindMatchingTextures()
        {
            foundTextures.Clear();

            Texture2D[] _matchingTextures = AssetsUtilities.GetAssetsOfType<Texture2D>(nameFilter, folderPath);

            foreach (var _texture in _matchingTextures)
            {
                if (_texture == null)
                {
                    continue;
                }

                if (_texture.name.StartsWith(prefix) && _texture.name.EndsWith(suffix))
                {
                    foundTextures.Add(_texture);
                }
            }
        }

        public void CreateTexturesList()
        {
            TexturesList = ScriptableObject.CreateInstance<Texture2DReorderableList>();
            TexturesList.Init(null, foundTextures);
        }

        public void DestroyTexturesList()
        {
            if (TexturesList != null)
            {
                TexturesList.Destroy();
            }
        }

        public void ClearFoundTextures()
        {
            foundTextures.Clear();
        }

        public void RemoveNullReferences()
        {
            for (int i = foundTextures.Count - 1; i >= 0; i--)
            {
                if (foundTextures[i] == null)
                {
                    foundTextures.RemoveAt(i);
                }
            }
        }

        public void FillWithWhiteTextures(int _maxCount)
        {
            while (foundTextures.Count < _maxCount)
            {
                foundTextures.Add(Texture2D.whiteTexture);
            }
        }

        public void FillWithGrayTextures(int _maxCount)
        {
            while (foundTextures.Count < _maxCount)
            {
                foundTextures.Add(Texture2D.grayTexture);
            }
        }

        public void FillWithBlackTextures(int _maxCount)
        {
            while (foundTextures.Count < _maxCount)
            {
                foundTextures.Add(Texture2D.blackTexture);
            }
        }

        public void FillWithCustomTextures(int _maxCount, Texture2D _texture)
        {
            if (_texture == null)
            {
                TextureChannelArranger.Error("Custom texture is null! Aborted filling the list.");
                return;
            }

            while (foundTextures.Count < _maxCount)
            {
                foundTextures.Add(_texture);
            }
        }

        public Texture2D GetFoundTextureAtIndex(int _index)
        {
            if (_index < 0 || _index > foundTextures.Count - 1)
            {
                return null;
            }

            return foundTextures[_index];
        }

        public bool IsAnyFoundTextureNull()
        {
            foreach (var _texture in foundTextures)
            {
                if (_texture == null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
