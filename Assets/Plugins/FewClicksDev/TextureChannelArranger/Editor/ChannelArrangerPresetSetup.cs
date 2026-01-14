namespace FewClicksDev.TextureChannelArranger
{
    using FewClicksDev.Core;
    using UnityEngine;
    using UnityEngine.Events;

    [System.Serializable]
    public class ChannelArrangerPresetSetup
    {
        public static event UnityAction OnAnySetupChanged = null;

        [SerializeField] private ChannelArrangerPreset preset = null;
        [SerializeField] private bool isVisible = false;
        [SerializeField] private int orderInTheList = 0;

        public ChannelArrangerPreset Preset => preset;
        public bool IsVisible => isVisible;
        public int OrderInTheList => orderInTheList;

        public ChannelArrangerPresetSetup(ChannelArrangerPreset _preset, bool _isVisible, int _orderInTheList)
        {
            preset = _preset;
            isVisible = _isVisible;
            orderInTheList = _orderInTheList;
        }

        public void SetOrderInTheList(int _order)
        {
            orderInTheList = _order;
        }

        public void SetVisibility(bool _isVisible)
        {
            isVisible = _isVisible;
            OnAnySetupChanged?.Invoke();
        }

        public string GetStringToSave()
        {
            return $"{Preset.GetAssetGUID()},{_getIntFromBool(IsVisible)},{OrderInTheList}";

            int _getIntFromBool(bool _bool) => _bool ? 1 : 0;
        }

        public static void ForceInvokeAnEvent()
        {
            OnAnySetupChanged?.Invoke();
        }
    }
}
