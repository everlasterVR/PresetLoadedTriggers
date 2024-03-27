#define ENV_DEVELOPMENT

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using UnityEngine;

namespace everlaster
{
    [SuppressMessage("ReSharper", "UnusedType.Global")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    static class DevUtils
    {
        #if ENV_DEVELOPMENT

        // ReSharper disable once UnusedMember.Global
        public static string ObjectPropertiesString(object obj)
        {
            var sb = new StringBuilder();

            sb.Append("Properties:\n");
            var properties = TypeDescriptor.GetProperties(obj);
            for(int i = 0; i < properties.Count; i++)
            {
                var property = properties[i];
                sb.AppendFormat("{0} = {1}\n", property.Name, property.GetValue(obj));
            }

            sb.Append("\nFields:\n");
            var fields = obj.GetType().GetFields();
            for(int i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                sb.AppendFormat("{0} = {1}\n", field.Name, field.GetValue(obj));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Get a string containing a visual representation of a Transform's children hierarchy.
        /// </summary>
        /// <param name="root">The parent Transform (GameObject)</param>
        /// <param name="maxDepth">Maximum depth to traverse the hierarchy.</param>
        /// <param name="propertyDel">A delegate function that takes a Transform and returns the string to print as an entry in the hierarchy.
        /// For example, to print a hierarchy of each transform's name: <code>thisTransform => thisTransform.name</code> A null value will print names.</param>
        /// <returns>A string containing a visual hierarchy of all child transforms, including the root</returns>
        // ReSharper disable once UnusedMember.Global
        public static string ObjectHierarchyToString(Transform root, int? maxDepth = null, Func<Transform, string> propertyDel = null)
        {
            if(propertyDel == null)
            {
                propertyDel = t => t.name;
            }

            var builder = new StringBuilder();
            ObjectHierarchyToString(root, propertyDel, builder, maxDepth);

            if(builder.Length < 1024)
            {
                return builder.ToString();
            }

            return
                $"Output string length {builder.Length.ToString()} may be too large for viewing in VAM. " +
                $"See %userprofile%/AppData/LocalLow/MeshedVR/VaM/output_log.txt for the full output.\n{builder}";
        }

        static void ObjectHierarchyToString(
            Transform root,
            Func<Transform, string> propertyDel,
            StringBuilder builder,
            int? maxDepth,
            int currentDepth = 0
        )
        {
            if(currentDepth > maxDepth)
            {
                return;
            }

            for(int i = 0; i < currentDepth; i++)
            {
                builder.Append("|   ");
            }

            builder.Append(propertyDel(root) + "\n");
            foreach(Transform child in root)
            {
                ObjectHierarchyToString(child, propertyDel, builder, maxDepth, currentDepth + 1);
            }
        }

        public static IEnumerable<string> GetChildComponentData(UnityEngine.Component component, int maxLevel = int.MaxValue) =>
            GetComponentHierarchy(component.transform, 0, maxLevel);

        static IEnumerable<string> GetComponentHierarchy(Transform current, int indentLevel, int maxLevel)
        {
            foreach(var childComponent in current.GetComponents<UnityEngine.Component>())
            {
                string indentation = "";
                for(int i = 0; i < indentLevel; i++)
                {
                    indentation += "> ";
                }

                string result = $"{indentation}[{childComponent.GetType()}]  '{childComponent.name}'";
                if(!childComponent.gameObject.activeSelf)
                {
                    result += " (inactive)";
                }

                yield return result;
            }

            if(indentLevel < maxLevel)
            {
                foreach(Transform child in current)
                {
                    foreach(string childData in GetComponentHierarchy(child, indentLevel + 1, maxLevel))
                    {
                        yield return childData;
                    }
                }
            }
        }

        public static IDictionary<string, string> GetChildComponentData(Transform component) =>
            GetComponentHierarchy(component, "");

        static IDictionary<string, string> GetComponentHierarchy(Transform current, string path)
        {
            var result = new Dictionary<string, string>();

            var components = current.GetComponents<UnityEngine.Component>();
            for(int i = 0; i < components.Length; i++)
            {
                string status = components[i].gameObject.activeSelf ? "active" : "inactive";
                result[$"{path}/{components[i].GetType()}[{i}]"] = status;
            }

            for(int i = 0; i < current.childCount; i++)
            {
                var childData = GetComponentHierarchy(current.GetChild(i), $"{path}/{current.GetChild(i).name}[{i}]");
                foreach(var pair in childData)
                {
                    result[pair.Key] = pair.Value;
                }
            }

            return result;
        }

        public static IDictionary<string, string> CompareComponentHierarchies(Transform a, Transform b)
        {
            var aData = GetChildComponentData(a);
            var bData = GetChildComponentData(b);
            var diff = new Dictionary<string, string>();

            foreach(var pair in aData)
            {
                if(!bData.ContainsKey(pair.Key))
                {
                    diff[pair.Key] = $"Present in A: {pair.Value}";
                }
                // else if (bData[pair.Key] != pair.Value)
                // {
                //     diff[pair.Key] = $"Different State A: {pair.Value} | B: {bData[pair.Key]}";
                // }
            }

            foreach(var pair in bData)
            {
                if(!aData.ContainsKey(pair.Key))
                {
                    diff[pair.Key] = $"Present in B: {pair.Value}";
                }
            }

            return diff;
        }

        public static IEnumerable<string> GetObjectLocations(
            Transform root,
            IEnumerable<UnityEngine.Component> components,
            Func<UnityEngine.Component, string> nameDel = null
        )
        {
            foreach(var component in components)
            {
                string path = nameDel?.Invoke(component) ?? component.name;
                var parent = component.transform.parent;
                while(parent != null && parent != root)
                {
                    path = parent.name + "/" + path;
                    parent = parent.parent;
                }

                yield return path;
            }
        }

        #else
        // ReSharper disable UnusedMember.Global
        public static string ObjectPropertiesString(object obj) => "";

        public static string CompareAndPrintObjectProperties(object objA, object objB) => "";

        public static string ObjectHierarchyToString(Transform root, int? maxDepth = null, Func<Transform, string> propertyDel = null) => "";

        public static IEnumerable<string> GetChildComponentData(UnityEngine.Component component) => new List<string>();

        public static IEnumerable<string> GetObjectLocations(
            Transform root,
            IEnumerable<UnityEngine.Component> components,
            Func<UnityEngine.Component, string> nameDel = null
        ) => new List<string>();

        // ReSharper restore UnusedMember.Global

        #endif
    }
}
