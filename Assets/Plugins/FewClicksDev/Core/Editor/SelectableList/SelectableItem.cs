namespace FewClicksDev.Core.SelectableList
{
    using UnityEngine;
    using UnityEngine.Events;

    [System.Serializable]
    public abstract class SelectableItem
    {
        public event UnityAction<SelectableItem> OnExpandStateChanged = null;

        public bool IsSelected { get; set; } = false;
        public Vector2 StartPositionAndHeight => startPositionAndHeight;

        public bool IsExpanded
        {
            get => isExpanded;

            set
            {
                isExpanded = value;
                OnExpandStateChanged?.Invoke(this);
            }
        }

        protected Vector2 startPositionAndHeight = Vector2.zero;
        protected bool isExpanded = false;

        public void RecalculateHeight()
        {
            RecalculateHeightAndStartPosition(StartPositionAndHeight.x);
        }

        public void ToggleExpandState()
        {
            IsExpanded = !isExpanded;
        }

        public void ToggleSelectedState()
        {
            IsSelected = !IsSelected;
        }

        public virtual void RecalculateHeightAndStartPosition(float _currentY)
        {
            startPositionAndHeight = new Vector2(_currentY, StartPositionAndHeight.y);
        }
    }
}
