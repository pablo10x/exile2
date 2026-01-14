namespace FewClicksDev.TextureChannelArranger
{
    using System.Collections.Generic;

    [System.Serializable]
    public struct InputConnection
    {
        public static InputConnection Empty = new InputConnection(-1, TextureChannelInput.Red, -1, TextureChannel.Red);

        public int InputTextureIndex;
        public TextureChannelInput InputChannel;
        public int OutputTextureIndex;
        public TextureChannel OutputChannel;

        public InputConnection(int _inputTextureIndex, TextureChannelInput _inputChannel)
        {
            InputTextureIndex = _inputTextureIndex;
            InputChannel = _inputChannel;
            OutputTextureIndex = -1;
            OutputChannel = TextureChannel.Red;
        }

        public InputConnection(int _inputTextureIndex, TextureChannelInput _inputChannel, int _outputTextureIndex, TextureChannel _outputChannel)
        {
            InputTextureIndex = _inputTextureIndex;
            InputChannel = _inputChannel;
            OutputTextureIndex = _outputTextureIndex;
            OutputChannel = _outputChannel;
        }

        public InputConnection GetCopy()
        {
            return new InputConnection(InputTextureIndex, InputChannel, OutputTextureIndex, OutputChannel);
        }

        public bool IsValid(List<TextureInput> _inputs, List<TextureOutput> _outputs)
        {
            bool _isInputInRange = InputTextureIndex >= 0 && InputTextureIndex < _inputs.Count;
            bool _isOutputInRange = OutputTextureIndex >= 0 && OutputTextureIndex < _outputs.Count;

            return _isInputInRange && _isOutputInRange;
        }

        public override string ToString()
        {
            return $"Connection from input texture at index {InputTextureIndex} and channel {InputChannel} to output texture at index {OutputTextureIndex} and channel {OutputChannel}";
        }
    }
}
