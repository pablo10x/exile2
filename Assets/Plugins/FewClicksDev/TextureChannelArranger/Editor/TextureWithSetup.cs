namespace FewClicksDev.TextureChannelArranger
{
    using UnityEngine;

    [System.Serializable]
    public struct TextureWithSetup
    {
        private const string NULL = "NULL";

        public Texture2D TextureReference;
        public ChannelSetup Setup;

        public TextureWithSetup(Texture2D _texture, ChannelSetup _setup)
        {
            TextureReference = _texture;
            Setup = _setup;
        }

        public string GetTextureName()
        {
            if (TextureReference == null)
            {
                return NULL;
            }

            return TextureReference.name;
        }
    }
}
