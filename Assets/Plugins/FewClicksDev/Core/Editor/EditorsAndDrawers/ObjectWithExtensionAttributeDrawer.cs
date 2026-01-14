namespace FewClicksDev.Core
{
    using System.IO;
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(ObjectWithExtensionAttribute), true)]
    public class ObjectWithExtensionAttributeDrawer : CustomPropertyDrawerBase
    {
        public override void OnGUI(Rect _position, SerializedProperty _property, GUIContent _label)
        {
            EditorGUI.BeginProperty(_position, _label, _property);
            ObjectWithExtensionAttribute _attribute = attribute as ObjectWithExtensionAttribute;

            string _correctExtension = _attribute.Extension;

            using (var _changeScope = new ChangeCheckScope())
            {
                EditorGUI.ObjectField(_position, _property, _label);

                if (_changeScope.changed)
                {
                    if (_property.objectReferenceValue != null)
                    {
                        string _path = AssetDatabase.GetAssetPath(_property.objectReferenceValue);
                        string _extension = Path.GetExtension(_path);

                        if (_extension != _correctExtension)
                        {
                            _property.objectReferenceValue = null;
                            BaseLogger.Error("OBJECT WITH EXTENSION", $"The object must have a {_correctExtension} extension.", EditorDrawer.RED);
                        }
                    }
                }
            }

            EditorGUI.EndProperty();
        }
    }
}
