namespace FewClicksDev.Core
{
    using UnityEditor;
    using UnityEngine;

    using static FewClicksDev.Core.EditorDrawer;

    public abstract class CustomInspectorBase : Editor
    {
        protected const float VERTICAL_SLIDER_WIDTH = 14f;

        protected virtual float leftPadding => SMALL_SPACE;
        protected virtual float rightPadding => NORMAL_SPACE;

        protected Vector2 scrollPosition = Vector2.zero;
        protected Color defaultGUIColor = Color.white;

        protected float inspectorWidth => EditorGUIUtility.currentViewWidth;
        protected float inspectorHeight => Screen.height;

        protected bool isVerticalSliderVisible { get; private set; }
        protected float verticalSliderWidth => isVerticalSliderVisible ? VERTICAL_SLIDER_WIDTH : 0f;

        protected float windowWidthWithLeftPadding => inspectorWidth - leftPadding;
        protected float windowWidthWithRightPadding => inspectorWidth - rightPadding;
        protected float windowWidthWithPaddings => inspectorWidth - leftPadding - rightPadding - verticalSliderWidth;

        protected virtual void OnEnable()
        {
            EditorApplication.update += Repaint;
        }

        protected virtual void OnDisable()
        {
            EditorApplication.update -= Repaint;
        }

        public override void OnInspectorGUI()
        {
            defaultGUIColor = GUI.color;

            using (var _scrollScope = new ScrollViewScope(scrollPosition))
            {
                scrollPosition = _scrollScope.scrollPosition;

                using (new HorizontalScope())
                {
                    Space(leftPadding);

                    using (new VerticalScope())
                    {
                        drawInspectorGUI();
                    }

                    Space(rightPadding - verticalSliderWidth);
                }

                checkForVisibleSlider();
            }
        }

        protected virtual void drawInspectorGUI()
        {
            drawDefaultInspector();
        }

        protected void drawDefaultInspector()
        {
            base.OnInspectorGUI();
        }

        protected void drawScript()
        {
            serializedObject.Update();
            serializedObject.DrawScriptProperty();
        }

        private void checkForVisibleSlider()
        {
            Rect _rect = GUILayoutUtility.GetLastRect();

            if (_rect.height > 1)
            {
                if (_rect.height > inspectorHeight)
                {
                    isVerticalSliderVisible = true;
                }
                else
                {
                    isVerticalSliderVisible = false;
                }
            }
        }
    }
}
