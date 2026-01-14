namespace FewClicksDev.TextureChannelArranger
{
    using FewClicksDev.Core;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    [CreateAssetMenu(menuName = "FewClicks Dev/Texture Channels Arranger/Preset", fileName = "arrangerPreset_NewChannelsArrangerPreset")]
    public class ChannelArrangerPreset : ScriptableObject
    {
        private const int MAX_INPUTS_AND_OUTPUTS = 4;

        [Header("Main settings")]
        [SerializeField] private string displayName = "New Preset";
        [TextArea(3, 5), SerializeField] private string description = string.Empty;
        [SerializeField] private int priority = 0;

        public string DisplayName => displayName;
        public string Description => description;
        public int Priority => priority;

        [Header("Setup")]
        [SerializeField] private List<TextureInput> inputs = new List<TextureInput>();
        [SerializeField] private List<TextureOutput> outputs = new List<TextureOutput>();

        public int NumberOfInputs => inputs != null ? inputs.Count : 0;
        public int NumberOfOutputs => outputs != null ? outputs.Count : 0;

        public List<TextureOutput> Outputs => outputs;

        private void OnEnable() //TODO move to the button
        {
            validateInputsAndOutputs();
        }

        public TextureInput GetInputCopyAtIndex(int _index)
        {
            return inputs[_index].GetCopy();
        }

        public ChannelSetup GetOutputChannelSetup(int _outputIndex, int _channelIndex)
        {
            return outputs[_outputIndex].GetChannelSetup(_channelIndex).GetCopy();
        }

        public TextureOutput GetOutputAtIndex(int _index)
        {
            return outputs[_index];
        }

        public TextureOutput GetOutputCopyAtIndex(int _index)
        {
            return outputs[_index].GetCopy();
        }

        public void CreatePresetFromTheWindow(string _displayName, List<TextureInput> _inputs, List<TextureOutput> _outputs, bool _clearInputTextures)
        {
            displayName = _displayName;

            Debug.Log($"Creating preset from the window, inputs: {_inputs.Count}, outputs: {_outputs.Count}");

            inputs = new List<TextureInput>();
            outputs = new List<TextureOutput>();

            for (int i = 0; i < _inputs.Count; i++)
            {
                inputs.Add(_inputs[i].GetCopy());
            }

            for (int i = 0; i < _outputs.Count; i++)
            {
                outputs.Add(_outputs[i].GetCopy());
            }

            if (_clearInputTextures)
            {
                foreach (var input in inputs)
                {
                    input.SetTexture(null);
                }
            }

            this.SetAsDirty();
        }

        private void validateInputsAndOutputs()
        {
            if (inputs.IsNullOrEmpty() == false && inputs.Count > MAX_INPUTS_AND_OUTPUTS)
            {
                while (inputs.Count > MAX_INPUTS_AND_OUTPUTS)
                {
                    inputs = inputs.Take(MAX_INPUTS_AND_OUTPUTS).ToList();
                }
            }

            if (outputs.IsNullOrEmpty() == false && outputs.Count > MAX_INPUTS_AND_OUTPUTS)
            {
                while (outputs.Count > MAX_INPUTS_AND_OUTPUTS)
                {
                    outputs = outputs.Take(MAX_INPUTS_AND_OUTPUTS).ToList();
                }
            }
        }
    }
}
