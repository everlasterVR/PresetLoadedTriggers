using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace everlaster
{
    static class ComponentExtensions
    {
        public static void SafeDestroyGameObject(this Component component)
        {
            if(component != null)
            {
                UnityEngine.Object.Destroy(component.gameObject);
            }
        }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    static class IEnumerableExtensions
    {
        public static string ToPrettyString<T>(this IEnumerable<T> enumerable, string separator = "\n")
        {
            var sb = new StringBuilder();
            foreach(var item in enumerable)
            {
                sb.Append(item);
                sb.Append(separator);
            }

            return sb.ToString();
        }
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    static class JSONClassExtensions
    {
        public static bool TryGetValue(this JSONClass jsonClass, string key, out JSONNode value)
        {
            if(jsonClass.HasKey(key))
            {
                value = jsonClass[key];
                return true;
            }

            value = null;
            return false;
        }

        public static bool TryGetValue(this JSONClass jsonClass, string key, out JSONClass value)
        {
            if(jsonClass.HasKey(key))
            {
                value = jsonClass[key].AsObject;
                return true;
            }

            value = null;
            return false;
        }

        public static bool TryGetValue(this JSONClass jsonClass, string key, out JSONArray value)
        {
            if(jsonClass.HasKey(key))
            {
                value = jsonClass[key].AsArray;
                return true;
            }

            value = null;
            return false;
        }

        public static bool TryGetValue(this JSONClass jsonClass, string key, out string value)
        {
            if(jsonClass.HasKey(key))
            {
                value = jsonClass[key].Value;
                return true;
            }

            value = null;
            return false;
        }

        public static bool TryGetValue(this JSONClass jsonClass, string key, out float value)
        {
            if(jsonClass.HasKey(key))
            {
                value = jsonClass[key].AsFloat;
                return true;
            }

            value = 0;
            return false;
        }

        public static bool TryGetValue(this JSONClass jsonClass, string key, out int value)
        {
            if(jsonClass.HasKey(key))
            {
                value = jsonClass[key].AsInt;
                return true;
            }

            value = 0;
            return false;
        }

        public static bool TryGetValue(this JSONClass jsonClass, string key, out bool value)
        {
            if(jsonClass.HasKey(key))
            {
                value = jsonClass[key].AsBool;
                return true;
            }

            value = false;
            return false;
        }
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    static class StringExtensions
    {
        public static string Bold(this string str) => $"<b>{str}</b>";

        public static string Italic(this string str) => $"<i>{str}</i>";

        public static string Size(this string str, int size) => $"<size={size.ToString()}>{str}</size>";

        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public static string Color(this string str, string color) => $"<color={color}>{str}</color>";

        public static string Color(this string str, Color color) => str.Color($"#{ColorUtility.ToHtmlStringRGB(color)}");

        public static Color ToColor(this string str)
        {
            Color color;
            ColorUtility.TryParseHtmlString(str, out color);
            return color;
        }
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    static class StringBuilderExtensions
    {
        public static StringBuilder AppendBold(this StringBuilder sb, string str) =>
            sb.AppendFormat("<b>{0}</b>", str);

        public static StringBuilder AppendItalic(this StringBuilder sb, string str) =>
            sb.AppendFormat("<i>{0}</i>", str);

        public static StringBuilder AppendSize(this StringBuilder sb, string str, int size) =>
            sb.AppendFormat("<size={0}>{1}</size>", size, str);

        public static StringBuilder AppendColor(this StringBuilder sb, string str, Color color) =>
            sb.AppendFormat("<color=#{0}>{1}</color>", ColorUtility.ToHtmlStringRGB(color), str);

        public static StringBuilder Clear(this StringBuilder sb)
        {
            sb.Length = 0;
            return sb;
        }
    }

    static class UIDynamicExtensions
    {
        public static void AddListener(this UIDynamic element, UnityAction callback)
        {
            if(!element)
            {
                return;
            }

            var uiDynamicButton = element as UIDynamicButton;
            if(!uiDynamicButton)
            {
                throw new ArgumentException($"UIDynamic {element.name} was null or not an UIDynamicButton");
            }

            uiDynamicButton.button.onClick.AddListener(callback);
        }
    }
}
