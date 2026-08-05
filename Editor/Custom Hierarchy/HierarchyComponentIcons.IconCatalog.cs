#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static partial class HierarchyComponentIcons
{
    internal static Texture GetBuiltInIcon(string iconName)
    {
        if (string.IsNullOrWhiteSpace(iconName))
        {
            return null;
        }

        string trimmedIconName = iconName.Trim();
        if (BuiltInIconCache.TryGetValue(trimmedIconName, out Texture cachedIcon))
        {
            return cachedIcon;
        }

        Texture icon = GetKnownIconFallback(trimmedIconName) ?? EditorGUIUtility.FindTexture(trimmedIconName);
        if (icon == null)
        {
            icon = GetIconContentTextureSilently(trimmedIconName);
        }

        if (icon == null && !trimmedIconName.StartsWith("d_", StringComparison.Ordinal))
        {
            string darkSkinIconName = "d_" + trimmedIconName;
            icon = EditorGUIUtility.FindTexture(darkSkinIconName);
            if (icon == null)
            {
                icon = GetIconContentTextureSilently(darkSkinIconName);
            }
        }

        BuiltInIconCache[trimmedIconName] = icon;
        return icon;
    }

    internal static Texture GetObjectIconTexture(GameObject gameObject, string iconName)
    {
        if (string.IsNullOrWhiteSpace(iconName))
        {
            return null;
        }

        if (IsComponentIconToken(iconName))
        {
            return GetComponentIconFromToken(gameObject, iconName);
        }

        return GetBuiltInIcon(iconName);
    }

    internal static string GetComponentIconToken(Component component)
    {
        if (component == null)
        {
            return string.Empty;
        }

        Type componentType = component.GetType();
        return ComponentIconTokenPrefix + componentType.AssemblyQualifiedName;
    }

    internal static string GetIconDisplayName(string iconName)
    {
        if (!IsComponentIconToken(iconName))
        {
            return iconName;
        }

        Type componentType = GetComponentTypeFromToken(iconName);
        return componentType != null ? ObjectNames.NicifyVariableName(componentType.Name) : "Missing Component Icon";
    }

    internal static bool IsIconSearchMatch(string iconName, string search)
    {
        string displayName = GetIconDisplayName(iconName);
        return string.IsNullOrWhiteSpace(search)
            || (!string.IsNullOrWhiteSpace(displayName) && displayName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
            || iconName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsComponentIconToken(string iconName)
    {
        return !string.IsNullOrWhiteSpace(iconName)
            && iconName.StartsWith(ComponentIconTokenPrefix, StringComparison.Ordinal);
    }

    private static Texture GetIconContentTextureSilently(string iconName)
    {
        bool previousLogEnabled = Debug.unityLogger.logEnabled;
        try
        {
            Debug.unityLogger.logEnabled = false;
            return EditorGUIUtility.IconContent(iconName).image;
        }
        finally
        {
            Debug.unityLogger.logEnabled = previousLogEnabled;
        }
    }

    private static Texture GetKnownIconFallback(string iconName)
    {
        switch (iconName)
        {
            case "UI.Dropdown Icon":
                return EditorGUIUtility.FindTexture("Dropdown Icon")
                    ?? EditorGUIUtility.FindTexture("Popup Icon")
                    ?? EditorGUIUtility.FindTexture("UI.Toggle Icon");
            case "UI.InputField Icon":
                return EditorGUIUtility.FindTexture("InputField Icon")
                    ?? EditorGUIUtility.FindTexture("TextField Icon")
                    ?? EditorGUIUtility.FindTexture("UI.Text Icon");
            case "MonoScript Icon":
                return EditorGUIUtility.FindTexture("cs Script Icon")
                    ?? EditorGUIUtility.FindTexture("ScriptableObject Icon");
            case "PhysicMaterial Icon":
                return EditorGUIUtility.FindTexture("PhysicsMaterial Icon")
                    ?? EditorGUIUtility.FindTexture("Material Icon");
            default:
                return null;
        }
    }

    private static Texture GetComponentIconFromToken(GameObject gameObject, string iconName)
    {
        Type componentType = GetComponentTypeFromToken(iconName);
        if (componentType == null)
        {
            return null;
        }

        Component component = gameObject != null ? gameObject.GetComponent(componentType) : null;
        Texture icon = component != null
            ? EditorGUIUtility.ObjectContent(component, componentType).image
            : null;

        return icon ?? EditorGUIUtility.ObjectContent(null, componentType).image;
    }

    private static Type GetComponentTypeFromToken(string iconName)
    {
        if (!IsComponentIconToken(iconName))
        {
            return null;
        }

        string typeName = iconName.Substring(ComponentIconTokenPrefix.Length);
        Type componentType = Type.GetType(typeName);
        if (componentType != null)
        {
            return componentType;
        }

        foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            componentType = assembly.GetType(typeName);
            if (componentType != null)
            {
                return componentType;
            }
        }

        return null;
    }
}
#endif

