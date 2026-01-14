namespace FewClicksDev.Core
{
    using System;
    using System.Reflection;
    using UnityEngine;

    public static class AssemblyExtensions
    {
        public const string ASSEMBLY_EXTENSIONS = "ASSEMBLY EXTENSIONS";

        public static readonly Color LOGS_COLOR = new Color(0.06051977f, 0.5680292f, 0.754717f, 1f);

        public static object InvokeInternalStaticMethod(Type _objectType, string _methodName, params object[] _params)
        {
            var _method = _objectType.GetMethod(_methodName, BindingFlags.NonPublic | BindingFlags.Static);

            if (_method == null)
            {
                BaseLogger.Error(ASSEMBLY_EXTENSIONS, $"{_methodName} doesn't exist in the {_objectType} type!", LOGS_COLOR);
                return null;
            }

            return _method.Invoke(null, _params);
        }

        public static object CallPrivateOrInternalMethod(this object _object, string _methodName, params object[] _args)
        {
            var _methodInfo = _object.GetType().GetMethod(_methodName, BindingFlags.NonPublic | BindingFlags.Instance);

            if (_methodInfo == null)
            {
                BaseLogger.Error(ASSEMBLY_EXTENSIONS, $"{_methodName} couldn't be found in the {_object}!", LOGS_COLOR);
                return null;
            }

            return _methodInfo.Invoke(_object, _args);
        }

        public static T GetPrivateOrInternalProperty<T>(this object _object, string _propertyName)
        {
            PropertyInfo _propertyInfo = _object.GetType().GetProperty(_propertyName, BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo _methodInfo = _propertyInfo.GetGetMethod(nonPublic: true);
            T _propertyValue = (T) _methodInfo.Invoke(_object, null);

            return _propertyValue;
        }
    }
}
