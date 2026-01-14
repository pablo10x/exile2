namespace FewClicksDev.Core
{
    using UnityEngine;

    [System.Serializable]
    public class MarkedObject<T> where T : Object
    {
        [SerializeField] protected T objectReference = default;
        [SerializeField] protected bool isMarked = false;

        public T ObjectReference => objectReference;
        public bool IsMarked => isMarked;

        public MarkedObject(T _object, bool _marked)
        {
            objectReference = _object;
            isMarked = _marked;
        }

        public bool Equals(T _object)
        {
            return objectReference == _object;
        }
    }
}
