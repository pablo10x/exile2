namespace FewClicksDev.Core
{
    using UnityEngine;

    public class ObjectWithExtensionAttribute : PropertyAttribute
    {
        [SerializeField] private string extension = string.Empty;

        public string Extension => extension;

        public ObjectWithExtensionAttribute(string _extension)
        {
            extension = _extension;
        }
    }
}
