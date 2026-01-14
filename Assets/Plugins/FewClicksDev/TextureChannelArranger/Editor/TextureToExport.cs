namespace FewClicksDev.TextureChannelArranger
{
    using System.Collections.Generic;
    using UnityEditor.Presets;
    using UnityEngine;

    using static TextureChannelArranger;

    [System.Serializable]
    public struct TextureToExport
    {
        public TextureOutput Output;
        public string ExportName;
        public string ExportFolderPath;
        public TextureExportFormat ExportFormat;
        public bool OverwriteExisting;
        public List<TextureWithSetup> Inputs;
        public Vector2Int TextureSize;
        public Preset PresetToApply;

        public TextureToExport(TextureOutput _output, string _exportName, string _folderPath, TextureExportFormat _format, Preset _preset, List<TextureWithSetup> _inputs)
        {
            Output = _output;
            ExportName = _exportName;
            ExportFolderPath = _folderPath;
            ExportFormat = _format;
            Inputs = _inputs;

            OverwriteExisting = false;
            TextureSize = Vector2Int.zero;
            PresetToApply = _preset;

            updateTextureSize();
        }

        public Texture2D GetTexture()
        {
            var _maxWidthAndHeight = TextureChannelArranger.GetMaxWidthAndHeight(Inputs);
            return GenerateOutputTexture(Output, _maxWidthAndHeight.Item1, _maxWidthAndHeight.Item2, Inputs);
        }

        public Texture2D GetPreviewTexture()
        {
            return GenerateOutputTexture(Output, PREVIEW_TEXTURE_WIDTH, PREVIEW_TEXTURE_WIDTH, Inputs);
        }

        private void updateTextureSize()
        {
            var _maxWidthAndHeight = TextureChannelArranger.GetMaxWidthAndHeight(Inputs);
            TextureSize = new Vector2Int(_maxWidthAndHeight.Item1, _maxWidthAndHeight.Item2);
        }
    }
}
