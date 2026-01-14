namespace FewClicksDev.Core
{
    using System.Collections.Generic;
    using System.Text;
    using UnityEngine;

    public static class Extensions
    {
        private const string NULL_STRING = "null";

        public static string InsertSpaceBeforeUpperCaseAndNumeric(this string _text)
        {
            return InsertBeforeUpperCaseAndNumeric(_text, " ");
        }

        public static string InsertBeforeUpperCaseAndNumeric(this string _text, string _toInsert)
        {
            if (_text.IsNullOrWhitespace())
            {
                return _text;
            }

            int _textLength = _text.Length;

            if (_textLength <= 1)
            {
                return _text;
            }

            StringBuilder _string = new StringBuilder();
            _string.Append(_text[0]);

            for (int i = 1; i < _textLength; i++)
            {
                if ((char.IsUpper(_text[i - 1]) == false && char.IsUpper(_text[i]))
                    || (char.IsDigit(_text[i - 1]) == false && char.IsDigit(_text[i]))
                    || (char.IsDigit(_text[i - 1]) && char.IsLetter(_text[i])))
                {
                    _string.Append(_toInsert);
                }

                _string.Append(_text[i]);
            }

            return _string.ToString();
        }

        public static float Remap(this float _value, float _from1, float _to1, float _from2, float _to2)
        {
            return (_value - _from1) / (_to1 - _from1) * (_to2 - _from2) + _from2;
        }

        public static float GetSumOfValues(this Vector3 _vector)
        {
            return _vector.x + _vector.y + _vector.z;
        }

        public static Vector3 GetSumOfValues(this Vector3[] _vectors)
        {
            Vector3 _sum = Vector3.zero;

            for (int i = 0; i < _vectors.Length; i++)
            {
                _sum += _vectors[i];
            }

            return _sum;
        }

        public static bool IsNullEmptyOrWhitespace(this string _text)
        {
            return _text.IsNullOrEmpty() || _text.IsNullOrWhitespace();
        }

        public static bool IsNullOrWhitespace(this string _text)
        {
            return string.IsNullOrWhiteSpace(_text);
        }

        public static bool IsNullOrEmpty(this string _text)
        {
            return string.IsNullOrEmpty(_text);
        }

        public static string TrimStart(this string _source, string _value)
        {
            if (_source.StartsWith(_value) == false)
            {
                return _source;
            }

            return _source.Substring(_value.Length);
        }

        public static string TrimEnd(this string _source, string _value)
        {
            if (_source.EndsWith(_value) == false)
            {
                return _source;
            }

            return _source.Remove(_source.LastIndexOf(_value));
        }

        public static string RemoveSpaces(this string _string)
        {
            return _string.Trim().Replace(" ", string.Empty);
        }

        public static string FirstLetterToUpperCase(this string _string)
        {
            if (_string.IsNullEmptyOrWhitespace())
            {
                return string.Empty;
            }

            return $"{_string[0].ToString().ToUpper()}{_string.Substring(1)}";
        }

        public static string GetNameIfNotNull(this Object _object)
        {
            if (_object == null)
            {
                return NULL_STRING;
            }

            return _object.name;
        }

        public static bool AddUnique<T>(this List<T> _list, T _object, bool _addAtTheEnd = false)
        {
            if (_list.Contains(_object) == false)
            {
                _list.Add(_object);

                return true;
            }

            if (_addAtTheEnd)
            {
                _list.Remove(_object);
                _list.Add(_object);

                return true;
            }

            return false;
        }

        public static string NumberToString(this int _number, int _stringLength = 2)
        {
            string _numberString = _number.ToString();

            if (_numberString.Length < _stringLength)
            {
                _numberString = _numberString.PadLeft(_stringLength, '0');

                return _numberString;
            }

            return _numberString;
        }

        public static string GetHexString(this Color _color, bool _withAlpha = false)
        {
            int _r = Mathf.RoundToInt(_color.r * 255f);
            int _g = Mathf.RoundToInt(_color.g * 255f);
            int _b = Mathf.RoundToInt(_color.b * 255f);

            if (_withAlpha == false)
            {
                return string.Format("#{0:X2}{1:X2}{2:X2}", _r, _g, _b);
            }

            int _a = Mathf.RoundToInt(_color.a * 255f);

            return string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", _r, _g, _b, _a);
        }

        public static bool IsSimilar(this Color _first, Color _toMatch, float _threshold)
        {
            return (_first.IsSimilarWithDifference(_toMatch, _threshold)).Item1;
        }

        public static (bool, float) IsSimilarWithDifference(this Color _first, Color _toMatch, float _threshold)
        {
            float _rDist = Mathf.Abs(_first.r - _toMatch.r);
            float _gDist = Mathf.Abs(_first.g - _toMatch.g);
            float _bDist = Mathf.Abs(_first.b - _toMatch.b);

            float _difference = _rDist + _gDist + _bDist;

            if (_difference > _threshold)
            {
                return (false, _difference);
            }

            return (true, _difference);
        }

        public static bool IsNullOrEmpty<T>(this List<T> _list)
        {
            return _list == null || _list.Count == 0;
        }

        public static bool IsNullOrEmpty<T>(this T[] _list)
        {
            return _list == null || _list.Length == 0;
        }

        public static bool IsNullOrEmpty<T>(this ICollection<T> _list)
        {
            return _list == null || _list.Count == 0;
        }

        public static T GetRandomElement<T>(this List<T> _list)
        {
            if (_list.IsNullOrEmpty())
            {
                return default;
            }

            return _list[Random.Range(0, _list.Count)];
        }

        public static void SwapElements<T>(this List<T> _list, int _index1, int _index2)
        {
            T _temp = _list[_index1];
            _list[_index1] = _list[_index2];
            _list[_index2] = _temp;
        }

        public static bool ContainsFlag<T>(this T _enum, T _flag) where T : System.Enum
        {
            return _enum.HasFlag(_flag);
        }

        public static string EveryWordToUpper(this string _text)
        {
            string[] _words = _text.Split(' ');

            for (int i = 0; i < _words.Length; i++)
            {
                if (_words[i].IsNullEmptyOrWhitespace())
                {
                    continue;
                }

                _words[i] = char.ToUpper(_words[i][0]) + _words[i].Substring(1);
            }

            return string.Join(" ", _words);
        }

        public static int IndexOf<T>(this T[] _array, T _item)
        {
            for (int i = 0; i < _array.Length; i++)
            {
                if (_array[i].Equals(_item))
                {
                    return i;
                }
            }

            return -1;
        }

        public static float GetMaxValue(this Vector2 _vector)
        {
            return Mathf.Max(_vector.x, _vector.y);
        }

        public static float GetMaxValue(this Vector3 _vector)
        {
            return Mathf.Max(_vector.x, _vector.y, _vector.z);
        }

        public static float GetMaxValue(this Vector4 _vector)
        {
            return Mathf.Max(_vector.x, _vector.y, _vector.z, _vector.w);
        }

        public static (bool, int) CheckForDuplicates<T>(List<T> _array, T _value) where T : Object
        {
            for (int i = 0; i < _array.Count; i++)
            {
                bool _isIn = _array[i] == _value;

                if (_isIn)
                {
                    return (true, i);
                }
            }

            return (false, -1);
        }

        public static T[] ExtractSubArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            System.Array.Copy(data, index, result, 0, length);

            return result;
        }

        public static string Combine<T>(this T[] _array, string _separator = "\n")
        {
            return string.Join(_separator, _array);
        }

        public static Color Clamp(this Color _color)
        {
            return new Color(Mathf.Clamp01(_color.r), Mathf.Clamp01(_color.g), Mathf.Clamp01(_color.b), Mathf.Clamp01(_color.a));
        }
    }
}