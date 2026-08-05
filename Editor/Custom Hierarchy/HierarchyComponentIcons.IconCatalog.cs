#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Module: Icon lookup and icon token handling.
// This file resolves Unity built-in icon names, applies version-compatible fallbacks for renamed icons,
// suppresses noisy missing-icon console messages, and converts component references into persistent
// component icon tokens that can be stored in EditorPrefs and resolved later.
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

        if (IsCustomIconToken(iconName))
        {
            return GetCustomIconFromToken(iconName);
        }

        return GetBuiltInIcon(iconName);
    }

    internal static bool TryImportCustomIconFromFile(string sourcePath, out string iconToken, out string errorMessage)
    {
        iconToken = string.Empty;
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            errorMessage = "The selected icon file does not exist.";
            return false;
        }

        string extension = Path.GetExtension(sourcePath).ToLowerInvariant();
        if (!IsSupportedCustomIconExtension(extension))
        {
            errorMessage = "Please choose a Unity-supported image file: png, jpg, jpeg, tga, psd, or bmp.";
            return false;
        }

        EnsureCustomIconImportFolder();
        string fileName = SanitizeAssetFileName(Path.GetFileNameWithoutExtension(sourcePath));
        string targetAssetPath = AssetDatabase.GenerateUniqueAssetPath(CustomIconImportFolder + "/" + fileName + extension);

        FileUtil.CopyFileOrDirectory(sourcePath, targetAssetPath);
        AssetDatabase.ImportAsset(targetAssetPath);
        ConfigureImportedCustomIcon(targetAssetPath);

        Texture2D importedIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(targetAssetPath);
        if (importedIcon == null)
        {
            AssetDatabase.DeleteAsset(targetAssetPath);
            errorMessage = "Unity could not import the selected file as a texture.";
            return false;
        }

        iconToken = CustomIconTokenPrefix + targetAssetPath;
        return true;
    }

    internal static List<string> GetImportedCustomIconTokens()
    {
        List<string> iconTokens = new List<string>();
        if (!AssetDatabase.IsValidFolder(CustomIconImportFolder))
        {
            return iconTokens;
        }

        string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { CustomIconImportFolder });
        foreach (string textureGuid in textureGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(textureGuid);
            if (!string.IsNullOrEmpty(assetPath))
            {
                iconTokens.Add(CustomIconTokenPrefix + assetPath);
            }
        }

        iconTokens.Sort((left, right) => string.Compare(
            GetIconDisplayName(left),
            GetIconDisplayName(right),
            StringComparison.OrdinalIgnoreCase));

        return iconTokens;
    }

    internal static int DeleteImportedCustomIcons(IEnumerable<string> iconTokens)
    {
        HashSet<string> assetPaths = new HashSet<string>();
        foreach (string iconToken in iconTokens)
        {
            string assetPath = GetCustomIconAssetPath(iconToken);
            if (string.IsNullOrEmpty(assetPath)
                || !assetPath.StartsWith(CustomIconImportFolder + "/", StringComparison.Ordinal))
            {
                continue;
            }

            assetPaths.Add(assetPath);
        }

        int deletedCount = 0;
        foreach (string assetPath in assetPaths)
        {
            if (AssetDatabase.DeleteAsset(assetPath))
            {
                deletedCount++;
            }
        }

        if (deletedCount > 0)
        {
            AssetDatabase.Refresh();
        }

        return deletedCount;
    }

    internal static string CustomIconFolderPath => CustomIconImportFolder;

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
        if (IsCustomIconToken(iconName))
        {
            string assetPath = GetCustomIconAssetPath(iconName);
            return Path.GetFileNameWithoutExtension(assetPath) + " (Custom)";
        }

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

    private static bool IsCustomIconToken(string iconName)
    {
        return !string.IsNullOrWhiteSpace(iconName)
            && iconName.StartsWith(CustomIconTokenPrefix, StringComparison.Ordinal);
    }

    private static Texture GetCustomIconFromToken(string iconName)
    {
        string assetPath = GetCustomIconAssetPath(iconName);
        return string.IsNullOrEmpty(assetPath)
            ? null
            : AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
    }

    private static string GetCustomIconAssetPath(string iconName)
    {
        return IsCustomIconToken(iconName)
            ? iconName.Substring(CustomIconTokenPrefix.Length)
            : string.Empty;
    }

    private static bool IsSupportedCustomIconExtension(string extension)
    {
        switch (extension)
        {
            case ".png":
            case ".jpg":
            case ".jpeg":
            case ".tga":
            case ".psd":
            case ".bmp":
                return true;
            default:
                return false;
        }
    }

    private static string SanitizeAssetFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "CustomHierarchyIcon";
        }

        foreach (char invalidChar in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(invalidChar, '_');
        }

        return string.IsNullOrWhiteSpace(fileName) ? "CustomHierarchyIcon" : fileName.Trim();
    }

    private static void EnsureCustomIconImportFolder()
    {
        if (AssetDatabase.IsValidFolder(CustomIconImportFolder))
        {
            return;
        }

        string[] folderParts = CustomIconImportFolder.Split('/');
        string currentPath = folderParts[0];
        for (int i = 1; i < folderParts.Length; i++)
        {
            string nextPath = currentPath + "/" + folderParts[i];
            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, folderParts[i]);
            }

            currentPath = nextPath;
        }
    }

    private static void ConfigureImportedCustomIcon(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
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

