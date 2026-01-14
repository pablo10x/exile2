namespace FewClicksDev.TextureChannelArranger
{
    [System.Serializable]
    public struct ChannelSetup
    {
        public TextureChannel Channel;
        public ChannelSource Source;
        public string TextureSetup;
        public float ColorGrayscale;
        public TextureChannelInput InputChannel;
        public bool Invert;

        public ChannelSetup(TextureChannel _channel)
        {
            Channel = _channel;
            Source = ChannelSource.None;
            TextureSetup = string.Empty;
            ColorGrayscale = 0;
            InputChannel = TextureChannelInput.Red;
            Invert = false;
        }

        public ChannelSetup(TextureChannel _channel, TextureChannelInput _inputChannel) : this(_channel)
        {
            InputChannel = _inputChannel;

            Source = ChannelSource.Texture;
            TextureSetup = string.Empty;
            ColorGrayscale = 0;
            Invert = false;
        }

        public ChannelSetup(TextureChannel _channel, TextureChannelInput _inputChannel, string _textureSetup) : this(_channel, _inputChannel)
        {
            TextureSetup = _textureSetup;
        }

        public ChannelSetup GetCopy()
        {
            return new ChannelSetup
            {
                Channel = Channel,
                Source = Source,
                TextureSetup = TextureSetup,
                ColorGrayscale = ColorGrayscale,
                InputChannel = InputChannel,
                Invert = Invert
            };
        }
    }
}
