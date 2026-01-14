namespace FewClicksDev.Core
{
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Helper class for assets management
    /// </summary>
    public static class AssetsUtilities
    {
        private const string UNITY_ENGINE_TYPE = "UnityEngine.";
        private const string UNITY_EDITOR_TYPE = "UnityEditor.";
        private const string ASSETS_FOLDER = "Assets";

        /// <summary>
        /// Returns the first found scriptable of type T with the given name
        /// </summary>
        public static T GetScriptableOfType<T>() where T : ScriptableObject
        {
            return GetAssetOfType<T>(string.Empty);
        }

        /// <summary>
        /// Returns the first asset of type T with the given name
        /// </summary>
        public static T GetAssetOfType<T>(string _name) where T : Object
        {
            string _type = $"{typeof(T)}";
            _type = FixObjectTypeForFilter(_type);

            string _searchFilter = $"t:{_type} {_name}";
            string[] _guids = AssetDatabase.FindAssets(_searchFilter);

            if (_guids.Length > 0)
            {
                string _assetPath = AssetDatabase.GUIDToAssetPath(_guids[0]);
                return AssetDatabase.LoadAssetAtPath<T>(_assetPath);
            }

            return null;
        }

        public static T[] GetAssetsOfType<T>(string _name = "", string _folder = "") where T : Object
        {
            string _type = $"{typeof(T)}";
            _type = FixObjectTypeForFilter(_type);

            string _searchFilter = $"t:{_type} {_name}";
            string[] _guids = null;

            if (_folder.IsNullEmptyOrWhitespace() == false)
            {
                _guids = AssetDatabase.FindAssets(_searchFilter, new string[] { ConvertAbsolutePathToDataPath(_folder) });
            }
            else
            {
                _guids = AssetDatabase.FindAssets(_searchFilter);
            }

            T[] _assets = new T[_guids.Length];
            int _index = 0;

            foreach (var _guid in _guids)
            {
                string _assetPath = AssetDatabase.GUIDToAssetPath(_guid);
                _assets[_index] = AssetDatabase.LoadAssetAtPath<T>(_assetPath);

                _index++;
            }

            return _assets;
        }

        /// <summary>
        /// Load an asset from GUID
        /// </summary>
        public static T LoadAsset<T>(string _guid) where T : Object
        {
            if (_guid.IsNullEmptyOrWhitespace())
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(_guid));
        }

        public static void SetAsDirty(this Object _object)
        {
            EditorUtility.SetDirty(_object);
        }

        /// <summary>
        /// Setting the dirty flag and saving the asset
        /// </summary>
        public static void SetDirtyAndSave(this Object _object)
        {
            EditorUtility.SetDirty(_object);
            AssetDatabase.SaveAssetIfDirty(_object);
        }

        /// <summary>
        /// Focusing on the Project window and pinging the object
        /// </summary>
        public static void Ping(Object _object)
        {
            if (_object == null)
            {
                return;
            }

            Selection.activeObject = _object;

            EditorUtility.FocusProjectWindow();
            EditorGUIUtility.PingObject(_object);
        }

        /// <summary>
        /// Returns asset path
        /// </summary>
        public static string GetAssetPath(this Object _object)
        {
            if (_object == null)
            {
                return string.Empty;
            }

            return AssetDatabase.GetAssetPath(_object);
        }

        /// <summary>
        /// Returns asset GUID
        /// </summary>
        public static string GetAssetGUID(this Object _object)
        {
            if (_object == null)
            {
                return string.Empty;
            }

            return AssetDatabase.AssetPathToGUID(_object.GetAssetPath());
        }

        /// <summary>
        /// Return folder path of the asset
        /// </summary>
        public static string GetFolderPath(this Object _object)
        {
            if (_object == null)
            {
                return string.Empty;
            }

            string _assetPath = AssetDatabase.GetAssetPath(_object);
            string _assetName = Path.GetFileName(_assetPath);
            int _length = _assetPath.Length - (_assetName.Length + 1);

            return _assetPath.Substring(0, _length);
        }

        public static string ConvertAbsolutePathToDataPath(string _path)
        {
            if (_path.StartsWith(Application.dataPath))
            {
                _path = _path.TrimStart(Application.dataPath);

                if (_path.StartsWith(ASSETS_FOLDER) == false)
                {
                    _path = ASSETS_FOLDER + _path;
                }
            }

            return _path;
        }

        public static string GetFileExtension(this Object _object)
        {
            if (_object == null)
            {
                return string.Empty;
            }

            string _assetPath = AssetDatabase.GetAssetPath(_object);
            return Path.GetExtension(_assetPath);
        }

        /// <summary>
        /// Converts absolute file path to the one that can be used in AssetDatabase
        /// </summary>
        /// <param name="_path">Absolute asset path</param>
        /// <param name="_fileName">File name</param>
        public static string ConvertAbsolutePathToDataPath(string _path, string _fileName)
        {
            if (_path.StartsWith(Application.dataPath))
            {
                _path = _path.TrimStart(Application.dataPath);
            }

            string _finalPath = string.Empty;

            if (_path.StartsWith(ASSETS_FOLDER) == false)
            {
                _finalPath += ASSETS_FOLDER;
            }

            _finalPath += $"{_path}/{_fileName}";

            return _finalPath;
        }

        public static Vector2Int GetTextureRealSize(this Texture _texture)
        {
            Vector2Int _size = Vector2Int.zero;

            if (_texture != null)
            {
                string _assetPath = AssetDatabase.GetAssetPath(_texture);
                TextureImporter _importer = AssetImporter.GetAtPath(_assetPath) as TextureImporter;

                if (_importer != null)
                {
                    object[] _args = new object[2] { 0, 0 };
                    MethodInfo _methodInfo = typeof(TextureImporter).GetMethod("GetWidthAndHeight", BindingFlags.NonPublic | BindingFlags.Instance);
                    _methodInfo.Invoke(_importer, _args);

                    _size.x = (int) _args[0];
                    _size.y = (int) _args[1];

                    return _size;
                }
            }

            return _size;
        }

        public static Vector2 GetBiggestTextureSize(List<Texture2D> _textures)
        {
            Vector2 _maxSize = Vector2.zero;
            float _lastSize = 0;

            for (int i = 0; i < _textures.Count; i++)
            {
                Vector2 _realTextureSize = _textures[i].GetTextureRealSize();
                float _size = _realTextureSize.x * _realTextureSize.y;

                if (_size > _lastSize)
                {
                    _lastSize = _size;
                    _maxSize = _realTextureSize;
                }
            }

            return _maxSize;
        }

        public static Texture2D Save(this Texture2D _texture, string _textureName, string _savePath)
        {
            if (Directory.Exists(_savePath) == false)
            {
                Directory.CreateDirectory(_savePath);
            }

            string _finalPath = $"{_savePath}/{_textureName}.png";

            FileStream _stream = new FileStream(_finalPath, FileMode.Create);
            BinaryWriter _binaryWriter = new BinaryWriter(_stream);

            _binaryWriter.Write(_texture.EncodeToPNG());
            _binaryWriter.Close();
            _stream.Close();

            AssetDatabase.Refresh();

            return AssetDatabase.LoadAssetAtPath<Texture2D>(_finalPath);
        }

        public static string FixObjectTypeForFilter(string _type)
        {
            if (_type.StartsWith(UNITY_ENGINE_TYPE))
            {
                _type = _type.TrimStart(UNITY_ENGINE_TYPE);
            }

            if (_type.StartsWith(UNITY_EDITOR_TYPE))
            {
                _type = _type.TrimStart(UNITY_EDITOR_TYPE);
            }

            return _type;
        }
    }
}