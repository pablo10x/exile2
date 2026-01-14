namespace FewClicksDev.TextureChannelArranger
{
    using FewClicksDev.Core;
    using System.Collections.Generic;
    using UnityEditor.Presets;
    using UnityEngine;

    using static TextureChannelArranger;

    [System.Serializable]
    public class TextureOutput : TextureReference
    {
        private const string TEXTURE_IMPORTER = "TextureImporter";
        private const string DEFAULT_OUTPUT_NAME = "Output";

        private const int MIN_DIGITS = 1;
        private const int MAX_DIGITS = 4;

        [SerializeField] private TextureExportMode exportMode = TextureExportMode.CreateNew;
        [SerializeField] private TextureNamingConvention exportNamingConvention = TextureNamingConvention.AddSuffix;
        [SerializeField] private Texture2D fileToOverwrite = null;
        [SerializeField] private string filePath = "Assets/";
        [SerializeField] private TextureImportMode importMode = TextureImportMode.BaseColorSRGB;
        [SerializeField] private Preset customPreset = null;

        [SerializeField] private BatchTextureNamingConvention batchExportNamingConvention = BatchTextureNamingConvention.NameWithIndex;
        [SerializeField] private string fileName = DEFAULT_OUTPUT_NAME;
        [SerializeField] private string exportName = DEFAULT_OUTPUT_NAME;
        [SerializeField] private string numberSeparator = "_";
        [SerializeField] private int numberOfDigits = 0;
        [SerializeField] private string exportPrefix = string.Empty;
        [SerializeField] private string exportSuffix = string.Empty;
        [SerializeField] private string connector = string.Empty;
        [SerializeField] private string exportPath = string.Empty;

        public string GeneratedName { get; set; }

        [SerializeField]
        private ChannelSetup[] channelSetups = new ChannelSetup[NUMBER_OF_CHANNELS] {
            new ChannelSetup(TextureChannel.Red),
            new ChannelSetup(TextureChannel.Green),
            new ChannelSetup(TextureChannel.Blue),
            new ChannelSetup(TextureChannel.Alpha)
        };

        public override bool IsSRGB => importMode is TextureImportMode.BaseColorSRGB;
        public override bool IsNormalMap => importMode is TextureImportMode.NormalMap;

        public override int NumberOfChannels => NUMBER_OF_CHANNELS;

        public TextureExportMode ExportMode
        {
            get => exportMode;
            set => exportMode = value;
        }

        public TextureNamingConvention ExportNamingConvention
        {
            get => exportNamingConvention;
            set => exportNamingConvention = value;
        }

        public Texture2D FileToOverwrite
        {
            get => fileToOverwrite;
            set => fileToOverwrite = value;
        }

        public string FileName
        {
            get => fileName;
            set => fileName = value;
        }

        public string FilePath
        {
            get => filePath;
            set => filePath = value;
        }

        public TextureImportMode ImportMode
        {
            get => importMode;
            set => importMode = value;
        }

        public Preset CustomPreset
        {
            get => customPreset;

            set
            {
                if (value != null && value.GetTargetTypeName() != TEXTURE_IMPORTER)
                {
                    return;
                }

                customPreset = value;
            }
        }

        public BatchTextureNamingConvention BatchExportNamingConvention
        {
            get => batchExportNamingConvention;
            set => batchExportNamingConvention = value;
        }

        public string ExportName
        {
            get => exportName;
            set => exportName = value;
        }

        public string NumberSeparator
        {
            get => numberSeparator;
            set => numberSeparator = value;
        }

        public int NumberOfDigits
        {
            get => numberOfDigits;
            set => numberOfDigits = Mathf.Clamp(value, MIN_DIGITS, MAX_DIGITS);
        }

        public string ExportPrefix
        {
            get => exportPrefix;
            set => exportPrefix = value;
        }

        public string ExportSuffix
        {
            get => exportSuffix;
            set => exportSuffix = value;
        }

        public string Connector
        {
            get => connector;
            set => connector = value;
        }

        public string ExportPath
        {
            get
            {
                if (exportPath.IsNullEmptyOrWhitespace())
                {
                    exportPath = "Assets";
                }

                return exportPath;
            }

            set => exportPath = value;
        }

        public Texture2D GeneratedTexture
        {
            get
            {
                if (reference == null)
                {
                    RegenerateTextureAsEmpty();
                }

                return reference;
            }
        }

        public ChannelSetup[] ChannelSetups => channelSetups;

        public TextureOutput()
        {
            textureName = DEFAULT_OUTPUT_NAME;
        }

        public TextureOutput(string _name)
        {
            textureName = _name;
        }

        public TextureOutput(TextureOutput _template)
        {
            textureName = _template.textureName;
            description = _template.description;
            exportMode = _template.exportMode;
            fileToOverwrite = _template.fileToOverwrite;
            fileName = _template.fileName;
            filePath = _template.filePath;
            importMode = _template.importMode;
            customPreset = _template.customPreset;

            for (int i = 0; i < NumberOfChannels; i++)
            {
                UpdateChannelSetup(i, _template.GetChannelSetup(i).GetCopy());
            }

            batchExportNamingConvention = _template.batchExportNamingConvention;
            exportName = _template.exportName;
            exportPrefix = _template.exportPrefix;
            exportSuffix = _template.exportSuffix;
            connector = _template.connector;
            exportPath = _template.exportPath;
        }

        public TextureOutput GetCopy()
        {
            return new TextureOutput(this);
        }

        public void RegenerateTextureAsEmpty()
        {
            reference = new Texture2D(4, 4, TextureFormat.ARGB32, false);
        }

        public void RegenerateTextureForPreview(List<TextureWithSetup> _inputs)
        {
            reference = GenerateOutputTexture(this, PREVIEW_TEXTURE_WIDTH, PREVIEW_TEXTURE_WIDTH, _inputs);
        }

        public ChannelSetup GetChannelSetup(int _index)
        {
            return channelSetups[_index];
        }

        public void UpdateChannelSetup(int _index, ChannelSetup _output)
        {
            channelSetups[_index] = _output;
        }

        public void UpdateGeneratedName(List<TextureInput> _inputs)
        {
            switch (batchExportNamingConvention)
            {
                case BatchTextureNamingConvention.NameWithIndex:
                    GeneratedName = ExportName + NumberSeparator + 1.NumberToString(NumberOfDigits);
                    break;

                case BatchTextureNamingConvention.AddPrefixToInput:
                    GeneratedName = ExportPrefix + _inputs[0].TextureName;
                    break;

                case BatchTextureNamingConvention.AddSuffixToInput:
                    GeneratedName = _inputs[0].TextureName + ExportSuffix;
                    break;

                case BatchTextureNamingConvention.CombineInputs:

                    GeneratedName = string.Empty;

                    foreach (var input in _inputs)
                    {
                        GeneratedName += input.TextureName;
                    }

                    break;
            }
        }

        public string GenerateName(List<TextureWithSetup> _texturesWithSetup, int _index)
        {
            switch (batchExportNamingConvention)
            {
                case BatchTextureNamingConvention.NameWithIndex:
                    return ExportName + NumberSeparator + (_index + 1).NumberToString(NumberOfDigits);

                case BatchTextureNamingConvention.AddPrefixToInput:
                    return ExportPrefix + _texturesWithSetup[0].GetTextureName();

                case BatchTextureNamingConvention.AddSuffixToInput:
                    return _texturesWithSetup[0].GetTextureName() + ExportSuffix;

                case BatchTextureNamingConvention.CombineInputs:

                    string _name = string.Empty;

                    for (int i = 0; i < _texturesWithSetup.Count; i++)
                    {
                        _name += _texturesWithSetup[i].GetTextureName();

                        if (i < _texturesWithSetup.Count - 1)
                        {
                            _name += Connector;
                        }
                    }

                    return _name;
            }

            return string.Empty;
        }

        public bool IsOutputValid()
        {
            bool _exportPathValid = exportPath.IsNullEmptyOrWhitespace() == false;
            bool _exportNameValid = GeneratedName.IsNullEmptyOrWhitespace() == false;

            return _exportPathValid && _exportNameValid;
        }

        public bool CanExportTexture()
        {
            switch (ExportMode)
            {
                case TextureExportMode.CreateNew:
                    return GeneratedName.IsNullEmptyOrWhitespace() == false && FilePath.IsNullEmptyOrWhitespace() == false;

                case TextureExportMode.Overwrite:
                    return FileToOverwrite != null;
            }

            return false;
        }

        public bool ShouldChannelBeDisabled(ChannelSetup _currentSetup)
        {
            return ImportMode switch
            {
                TextureImportMode.NormalMap => _currentSetup.Channel == TextureChannel.Alpha,
                TextureImportMode.SingleChannel => _currentSetup.Channel != TextureChannel.Red,
                _ => false
            };
        }
    }
}
