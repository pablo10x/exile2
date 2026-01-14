namespace FewClicksDev.TextureChannelArranger
{
    using FewClicksDev.Core;
    using System.Collections.Generic;
    using System.IO;
    using UnityEditor;
    using UnityEditor.Presets;
    using UnityEngine;
    using UnityEngine.Experimental.Rendering;

    using Preferences = ChannelArrangerUserPreferences;

    public enum TextureExportMode
    {
        CreateNew = 0,
        Overwrite = 1
    }

    public enum TextureAddMode
    {
        Manual = 0,
        Search = 1
    }

    public enum TextureExportFormat
    {
        PNG = 0,
        TGA = 1,
        JPG = 2
    }

    public enum TextureChannel
    {
        Red = 0,
        Green = 1,
        Blue = 2,
        Alpha = 3
    }

    public enum TextureChannelInput
    {
        Red = 0,
        Green = 1,
        Blue = 2,
        Alpha = 3,
        RGB = 4
    }

    public enum ChannelSource
    {
        None = 0,
        Texture = 1,
        SolidColor = 2
    }

    public enum ChannelPreviewMode
    {
        Default = 0,
        Grayscale = 1
    }

    public enum TextureNamingConvention
    {
        AddPrefix = 0,
        AddSuffix = 1,
        Custom = 2
    }

    public enum BatchTextureNamingConvention
    {
        NameWithIndex = 0,
        AddPrefixToInput = 1,
        AddSuffixToInput = 2,
        CombineInputs = 3,
        Custom = 4
    }

    public enum TextureImportMode
    {
        BaseColorSRGB = 0,
        BaseColorLinear = 1,
        NormalMap = 2,
        SingleChannel = 3,
        Custom = 4
    }

    public static class TextureChannelArranger
    {
        public const string NAME = "Texture Channel Arranger";
        public const string CAPS_NAME = "TEXTURE CHANNEL ARRANGER";
        public const string VERSION = "1.0.2";

        public static readonly Color MAIN_COLOR = new Color(0.177287f, 0.45283f, 0.398177f, 1f);
        public static readonly Color LOGS_COLOR = new Color(0.267266f, 0.735849f, 0.648827f, 1f);

        public static readonly string[] CHANNELS = new string[] { "R", "G", "B", "A", "Combined RGB" };
        public static readonly string[] SUPPORTED_FILE_FORMATS = new string[] { PNG_EXTENSION, TGA_EXTENSION, JPG_EXTENSION };

        public const int NUMBER_OF_CHANNELS = 4;
        public const int PREVIEW_TEXTURE_WIDTH = 256;

        public static Shader CustomBlitShader
        {
            get
            {
                if (customBlitShader == null)
                {
                    customBlitShader = Shader.Find(CUSTOM_BLIT_SHADER_NAME);
                }

                return customBlitShader;
            }
        }

        public static Shader PreviewShader
        {
            get
            {
                if (previewShader == null)
                {
                    previewShader = Shader.Find(PREVIEW_SHADER_NAME);
                }

                return previewShader;
            }
        }

        public static Material BlitMaterial => blitMaterial;

        public static Material PreviewMaterial
        {
            get
            {
                if (previewMaterial == null)
                {
                    previewMaterial = new Material(PreviewShader);
                }

                return previewMaterial;
            }
        }

        private const string CUSTOM_BLIT_SHADER_NAME = "Hidden/FewClicksDev/sh_CustomBlit";
        private const string PREVIEW_SHADER_NAME = "Hidden/FewClicksDev/sh_Preview";

        private const string FIRST_PROPERTY = "{0}";
        private const string SECOND_PROPERTY = "{1}";

        private const string TEXTURE_NAME_PROPERTY = "_input_{0}";
        private const string TEXTURE_SATURATION_PROPERTY = "_input_{0}_saturation";
        private const string CHANNEL_MIXER_PROPERTY = "_in_{0}_{1}";
        private const string CHANNEL_INVERT_PROPERTY = "_input_{0}_ch_invert";
        private const string TEXTURE_OUTPUT_PROPERTY = "_isOutputTex";
        private const string SOLID_COLOR_PROPERTY = "_solidGrayscale";
        private const string IS_NORMAL_PROPERTY = "_isNormal";
        private const string IS_SRGB_PROPERTY = "_is_sRGB";

        private const string PNG_EXTENSION = ".png";
        private const string TGA_EXTENSION = ".tga";
        private const string JPG_EXTENSION = ".jpg";

        private static Material blitMaterial = null;
        private static Material previewMaterial = null;

        private static Shader customBlitShader = null;
        private static Shader previewShader = null;

        public static void Log(string _message)
        {
            if (Preferences.PrintLogs == false)
            {
                return;
            }

            BaseLogger.Log(CAPS_NAME, _message, LOGS_COLOR);
        }

        public static void Warning(string _message)
        {
            if (Preferences.PrintLogs == false)
            {
                return;
            }

            BaseLogger.Warning(CAPS_NAME, _message, LOGS_COLOR);
        }

        public static void Error(string _message)
        {
            if (Preferences.PrintLogs == false)
            {
                return;
            }

            BaseLogger.Error(CAPS_NAME, _message, LOGS_COLOR);
        }

        public static void SaveTheTexture(string _folderPath, Texture2D _texture, string _fileName, GraphicsFormat _format, TextureExportFormat _exportFormat, Preset _preset, bool _ping)
        {
            string _filePath = $"{_folderPath}/{_fileName}{getFileExtension(_exportFormat)}";
            _filePath = AssetsUtilities.ConvertAbsolutePathToDataPath(_filePath);
            byte[] _pixelsData = null;

            switch (_exportFormat)
            {
                case TextureExportFormat.PNG:
                    _pixelsData = ImageConversion.EncodeArrayToPNG(_texture.GetRawTextureData(), _format, (uint) _texture.width, (uint) _texture.height);
                    break;

                case TextureExportFormat.TGA:
                    _pixelsData = _texture.EncodeToTGA();
                    break;

                case TextureExportFormat.JPG:
                    _pixelsData = _texture.EncodeToJPG();
                    break;
            }

            File.WriteAllBytes(_filePath, _pixelsData);
            AssetDatabase.Refresh();

            Texture2D _loaded = AssetDatabase.LoadAssetAtPath<Texture2D>(_filePath);

            if (_preset != null)
            {
                string _texturePath = AssetDatabase.GetAssetPath(_loaded);
                TextureImporter _importer = AssetImporter.GetAtPath(_texturePath) as TextureImporter;

                if (_importer != null)
                {
                    _preset.ApplyTo(_importer);
                    _importer.SaveAndReimport();
                }
            }

            if (_ping)
            {
                AssetsUtilities.Ping(_loaded);
            }
        }

        public static Texture2D GenerateOutputTexture(TextureOutput _output, int _width, int _height, List<TextureWithSetup> _inputs)
        {
            if (_inputs.Count < 1)
            {
                return Texture2D.grayTexture;
            }

            setupBlitMaterial(_inputs);
            TextureFormat _format = _getFormatFromOutput();
            RenderTextureFormat _renderTextureFormat = _format is TextureFormat.R8 ? RenderTextureFormat.R8 : RenderTextureFormat.ARGB32;

            Texture2D _generatedTexture = new Texture2D(_width, _height, _format, false);
            RenderTexture _previous = RenderTexture.active;
            RenderTexture _tempRT = RenderTexture.GetTemporary(_width, _height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Graphics.Blit(Texture2D.whiteTexture, _tempRT, blitMaterial);
            RenderTexture.active = _tempRT;

            _generatedTexture.ReadPixels(new Rect(0, 0, _tempRT.width, _tempRT.height), 0, 0);
            _generatedTexture.Apply();

            RenderTexture.active = _previous;
            RenderTexture.ReleaseTemporary(_tempRT);

            return _generatedTexture;

            TextureFormat _getFormatFromOutput()
            {
                if (_inputs.Count == 1)
                {
                    return TextureFormat.R8;
                }

                return _output.ImportMode switch
                {
                    TextureImportMode.BaseColorSRGB => _hasAlpha() ? TextureFormat.RGBA32 : TextureFormat.RGB24,
                    TextureImportMode.BaseColorLinear => _hasAlpha() ? TextureFormat.RGBA32 : TextureFormat.RGB24,
                    TextureImportMode.NormalMap => TextureFormat.RGBA32,
                    TextureImportMode.SingleChannel => TextureFormat.R8,
                    _ => TextureFormat.RGBA32
                };
            }

            bool _hasAlpha()
            {
                foreach (var _input in _inputs)
                {
                    if (_input.Setup.Channel is TextureChannel.Alpha)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public static (int, int) GetMaxWidthAndHeight(List<TextureWithSetup> _inputs)
        {
            int _maxWidth = 0;
            int _maxHeight = 0;

            foreach (TextureWithSetup _input in _inputs)
            {
                if (_input.TextureReference == null)
                {
                    continue;
                }

                if (_input.TextureReference.width > _maxWidth)
                {
                    _maxWidth = _input.TextureReference.width;
                }

                if (_input.TextureReference.height > _maxHeight)
                {
                    _maxHeight = _input.TextureReference.height;
                }
            }

            return (_maxWidth, _maxHeight);
        }

        public static Color GetColorFromChannel(TextureChannel _channel)
        {
            return _channel switch
            {
                TextureChannel.Red => Color.red,
                TextureChannel.Green => Color.green,
                TextureChannel.Blue => Color.blue,
                TextureChannel.Alpha => Color.white,
                _ => Color.black,
            };
        }

        public static TextureExportFormat GetFormatFromString(string _extension)
        {
            return _extension switch
            {
                PNG_EXTENSION => TextureExportFormat.PNG,
                TGA_EXTENSION => TextureExportFormat.TGA,
                JPG_EXTENSION => TextureExportFormat.JPG,
                _ => TextureExportFormat.PNG,
            };
        }

        private static void setupBlitMaterial(List<TextureWithSetup> _inputs)
        {
            blitMaterial = new Material(CustomBlitShader); //To reset all the properties
            blitMaterial.hideFlags = HideFlags.HideAndDontSave;

            for (int i = 0; i < _inputs.Count; i++)
            {
                if (_inputs[i].Setup.Source != ChannelSource.Texture)
                {
                    continue;
                }

                blitMaterial.SetTexture(_getInputName(_inputs[i].Setup.Channel), _inputs[i].TextureReference);

                string _property = _getChannelPropertyName(i, _inputs[i].Setup.Channel);
                Vector4 _channelVector = _getChannelVector(_inputs[i].TextureReference, _inputs[i].Setup.InputChannel);
                blitMaterial.SetVector(_property, _channelVector);

                string _invertPropertyName = _getChannelInvertPropertyName(i);
                Vector4 _invertVector = _getChannelInvertVector();
                blitMaterial.SetVector(_invertPropertyName, _invertVector);

                if (_inputs[i].Setup.InputChannel is TextureChannelInput.RGB)
                {
                    string _saturationProperty = _getInputSaturationName(_inputs[i].Setup.Channel);
                    blitMaterial.SetFloat(_saturationProperty, 0f);
                }
            }

            blitMaterial.SetVector(TEXTURE_OUTPUT_PROPERTY, _getTextureOutputVector());
            blitMaterial.SetVector(SOLID_COLOR_PROPERTY, _getSolidColorVector());
            blitMaterial.SetVector(IS_NORMAL_PROPERTY, _getIsNormalVector());
            blitMaterial.SetVector(IS_SRGB_PROPERTY, _getIsSRGBVector());

            string _getInputName(TextureChannel _channel)
            {
                return TEXTURE_NAME_PROPERTY.Replace(FIRST_PROPERTY, _getIndexFromChannel(_channel).ToString());
            }

            string _getInputSaturationName(TextureChannel _channel)
            {
                return TEXTURE_SATURATION_PROPERTY.Replace(FIRST_PROPERTY, _getIndexFromChannel(_channel).ToString());
            }

            string _getChannelPropertyName(int _inputIndex, TextureChannel _channel)
            {
                return string.Format(CHANNEL_MIXER_PROPERTY.Replace(FIRST_PROPERTY, (_inputIndex + 1).ToString()).Replace(SECOND_PROPERTY, _getIndexFromChannel(_channel).ToString()));
            }

            string _getChannelInvertPropertyName(int _inputIndex)
            {
                return string.Format(CHANNEL_INVERT_PROPERTY.Replace(FIRST_PROPERTY, (_inputIndex + 1).ToString()));
            }

            Vector4 _getChannelVector(Texture2D _texture, TextureChannelInput _channel)
            {
                return _channel switch
                {
                    TextureChannelInput.Red => new Vector4(1f, 0f, 0f, 0f),
                    TextureChannelInput.Green => new Vector4(0f, 1f, 0f, 0f),
                    TextureChannelInput.Blue => new Vector4(0f, 0f, 1f, 0f),
                    TextureChannelInput.Alpha => new Vector4(0f, 0f, 0f, 1f),
                    TextureChannelInput.RGB => new Vector4(1f, 0f, 0f, 0f), // Desaturated RGB has all channels the same
                    _ => Vector4.zero,
                };
            }

            Vector4 _getChannelInvertVector()
            {
                Vector4 _vector = Vector4.zero;

                foreach (var _input in _inputs)
                {
                    if (_input.Setup.Source is ChannelSource.Texture && _input.Setup.Invert)
                    {
                        _vector[_getVectorIndexFromChannel(_input.Setup.Channel)] = 1f;
                    }
                }

                return _vector;
            }

            Vector4 _getTextureOutputVector()
            {
                Vector4 _vector = Vector4.zero;

                foreach (var _input in _inputs)
                {
                    if (_input.Setup.Source is ChannelSource.Texture)
                    {
                        _vector[_getVectorIndexFromChannel(_input.Setup.Channel)] = 1f;
                    }
                }

                return _vector;
            }

            Vector4 _getSolidColorVector()
            {
                Vector4 _colors = Vector4.zero;

                foreach (var _input in _inputs)
                {
                    if (_input.Setup.Source != ChannelSource.Texture)
                    {
                        _colors[_getVectorIndexFromChannel(_input.Setup.Channel)] = _input.Setup.ColorGrayscale;
                    }
                }

                return _colors;
            }

            Vector4 _getIsNormalVector()
            {
                Vector4 _isNormal = Vector4.zero;

                foreach (var _input in _inputs)
                {
                    if (_input.Setup.Source is ChannelSource.Texture)
                    {
                        TextureImporter _importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(_input.TextureReference)) as TextureImporter;

                        if (_importer != null && _importer.textureType is TextureImporterType.NormalMap)
                        {
                            _isNormal[_getVectorIndexFromChannel(_input.Setup.Channel)] = 1f;
                        }
                    }
                }

                return _isNormal;
            }

            Vector4 _getIsSRGBVector()
            {
                Vector4 _isSRGB = Vector4.zero;

                foreach (var _input in _inputs)
                {
                    if (_input.Setup.Source is ChannelSource.Texture)
                    {
                        TextureImporter _importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(_input.TextureReference)) as TextureImporter;

                        if (_importer != null && _importer.sRGBTexture)
                        {
                            _isSRGB[_getVectorIndexFromChannel(_input.Setup.Channel)] = 1f;
                        }
                    }
                }

                return _isSRGB;
            }

            int _getIndexFromChannel(TextureChannel _channel)
            {
                return _channel switch
                {
                    TextureChannel.Red => 1,
                    TextureChannel.Green => 2,
                    TextureChannel.Blue => 3,
                    TextureChannel.Alpha => 4,
                    _ => 1,
                };
            }

            int _getVectorIndexFromChannel(TextureChannel _channel)
            {
                return _channel switch
                {
                    TextureChannel.Red => 0,
                    TextureChannel.Green => 1,
                    TextureChannel.Blue => 2,
                    TextureChannel.Alpha => 3,
                    _ => 0,
                };
            }
        }

        private static string getFileExtension(TextureExportFormat _exportFormat)
        {
            return _exportFormat switch
            {
                TextureExportFormat.PNG => PNG_EXTENSION,
                TextureExportFormat.TGA => TGA_EXTENSION,
                TextureExportFormat.JPG => JPG_EXTENSION,
                _ => PNG_EXTENSION,
            };
        }
    }
}
