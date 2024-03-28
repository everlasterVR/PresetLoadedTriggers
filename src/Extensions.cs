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
    static class GameObjectExtensions
    {
        public static T AddComponent<T>(this GameObject go, Action<T> callback) where T : MonoBehaviour
        {
            var component = go.AddComponent<T>();
            callback(component);
            return component;
        }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    static class IEnumerableExtensions
    {
        public static string ToPrettyString<T>(this IEnumerable<T> enumerable, string separator = "\n")
            => string.Join(separator, enumerable.Select(item => item.ToString()).ToArray());
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

    static class JSONStorableExtensions
    {
        public static bool IsEnabledSafe(this JSONStorable storable) => storable && storable.enabled;
    }

    static class MVRScriptExtensions
    {
        public static string GetPackagePath(this MVRScript script)
        {
            string packageId = script.GetPackageId();
            return packageId == null ? "" : $"{packageId}:/";
        }

        public static string GetPackageId(this MVRScript script)
        {
            string id = script.name.Substring(0, script.name.IndexOf('_'));
            string filename = script.manager.GetJSON()["plugins"][id].Value;
            return FileUtils.ParsePackageIdFromPath(filename);
        }

        public static bool IsDuplicate(this MVRScript script)
        {
            var pluginPrefixRegex = Utils.NewRegex(@"^plugin#\d+_");
            var scripts = script.manager.GetComponentsInChildren<MVRScript>();
            for(int i = 0; i < scripts.Length; i++)
            {
                var otherScript = scripts[i];
                if(otherScript == script)
                {
                    continue;
                }

                if(pluginPrefixRegex.Replace(script.storeId, "") == pluginPrefixRegex.Replace(otherScript.storeId, ""))
                {
                    return true;
                }
            }

            return false;
        }

        public static Transform InstantiateTextField(this MVRScript script, Transform parent = null) =>
            Instantiate(script.manager.configurableTextFieldPrefab, parent);

        public static Transform InstantiateButton(this MVRScript script, Transform parent = null) =>
            Instantiate(script.manager.configurableButtonPrefab, parent);

        public static Transform InstantiateSlider(this MVRScript script, Transform parent = null) =>
            Instantiate(script.manager.configurableSliderPrefab, parent);

        public static Transform InstantiateToggle(this MVRScript script, Transform parent = null) =>
            Instantiate(script.manager.configurableTogglePrefab, parent);

        public static Transform InstantiatePopup(this MVRScript script, Transform parent = null) =>
            Instantiate(script.manager.configurablePopupPrefab, parent);

        public static Transform InstantiateScrollablePopup(this MVRScript script, Transform parent = null) =>
            Instantiate(script.manager.configurableScrollablePopupPrefab, parent);

        public static Transform InstantiateFilterablePopup(this MVRScript script, Transform parent = null) =>
            Instantiate(script.manager.configurableFilterablePopupPrefab, parent);

        public static Transform InstantiateColorPicker(this MVRScript script, Transform parent = null) =>
            Instantiate(script.manager.configurableColorPickerPrefab, parent);

        public static Transform InstantiateSpacer(this MVRScript script, Transform parent = null) =>
            Instantiate(script.manager.configurableSpacerPrefab, parent);

        static Transform Instantiate(Transform prefab, Transform parent = null)
        {
            var transform = UnityEngine.Object.Instantiate(prefab, parent, false);
            UnityEngine.Object.Destroy(transform.GetComponent<LayoutElement>());
            return transform;
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

    static class UIPopupExtensions
    {
        const int MAX_VISIBLE_COUNT = 400;

        public static void SetPreviousOrLastValue(this UIPopup uiPopup)
        {
            if(uiPopup.currentValue == uiPopup.popupValues[0])
            {
                uiPopup.currentValue = uiPopup.LastVisibleValue();
            }
            else
            {
                uiPopup.SetPreviousValue();
            }
        }

        public static void SetNextOrFirstValue(this UIPopup uiPopup)
        {
            if(uiPopup.currentValue == uiPopup.LastVisibleValue())
            {
                uiPopup.currentValue = uiPopup.popupValues[0];
            }
            else
            {
                uiPopup.SetNextValue();
            }
        }

        static string LastVisibleValue(this UIPopup uiPopup) => uiPopup.popupValues.Length > MAX_VISIBLE_COUNT
            ? uiPopup.popupValues[MAX_VISIBLE_COUNT - 1]
            : uiPopup.popupValues.Last();
    }

    [SuppressMessage("ReSharper", "UnusedType.Global")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    static class Vector3Extensions
    {
        public static string ToPrettyString(this Vector3 vector, string format = "0.000") =>
            $"({vector.x.ToString(format)}, {vector.y.ToString(format)}, {vector.z.ToString(format)})";
    }

    [SuppressMessage("ReSharper", "UnusedType.Global")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    static class Vector2Extensions
    {
        public static string ToPrettyString(this Vector2 vector, string format = "0.000") =>
            $"({vector.x.ToString(format)}, {vector.y.ToString(format)})";
    }
}
