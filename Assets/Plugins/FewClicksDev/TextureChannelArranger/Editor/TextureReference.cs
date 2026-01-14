namespace FewClicksDev.TextureChannelArranger
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Rendering;

    [System.Serializable]
    public abstract class TextureReference
    {
        [SerializeField] protected string textureName = "Input";
        [SerializeField] protected string description = string.Empty;

        [SerializeField] protected Texture2D reference = null;
        [SerializeField] protected bool isNormal = false;
        [SerializeField] protected bool isSRGB = false;

        [SerializeField] protected ColorWriteMask previewMask = ColorWriteMask.All;

        private List<Rect> channelsRects = new List<Rect>();

        public Texture2D Reference => reference;
        public virtual bool IsSRGB => isSRGB;
        public virtual bool IsNormalMap => isNormal;
        public bool IsLinear => IsSRGB == false;

        public ColorWriteMask PreviewMask => previewMask;
        public string TextureName => textureName;
        public string Description => description;

        public abstract int NumberOfChannels { get; }
        public Rect BatchConvertRect { get; set; }

        public Rect GetChannelRect(int _index)
        {
            while (_index > channelsRects.Count - 1)
            {
                channelsRects.Add(new Rect());
            }

            return channelsRects[_index];
        }

        public void SetChannelRect(int _index, Rect _rect)
        {
            while (_index > channelsRects.Count - 1)
            {
                channelsRects.Add(new Rect());
            }

            channelsRects[_index] = _rect;
        }

        public void UpdateTexturePreviewMask(ColorWriteMask _mask)
        {
            if (_mask == previewMask)
            {
                previewMask = ColorWriteMask.All;
                return;
            }

            previewMask = _mask;
        }
    }
}
