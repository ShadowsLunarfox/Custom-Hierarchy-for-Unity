#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Module: EditorPrefs persistence and cleanup.
// This file stores per-object style values, tracks all generated preference keys, removes stale records,
// clears Unity object icons when needed, and provides helper routines for building or pruning stored keys.
// It intentionally keeps storage mechanics separate from drawing and user-interface code.
public static partial class HierarchyComponentIcons
{
    private static void SetObjectRowColor(GameObject gameObject, Color color)
    {
        string key = GetObjectRowColorKey(gameObject);
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        EditorPrefs.SetString(key, ColorUtility.ToHtmlStringRGBA(color));
        TrackObjectColorKey(key);
    }

    private static void ClearObjectRowColor(GameObject gameObject)
    {
        string key = GetObjectRowColorKey(gameObject);
        if (!string.IsNullOrEmpty(key))
        {
            EditorPrefs.DeleteKey(key);
            ForgetObjectColorKey(key);
        }
    }

    private static void SetObjectLineColor(GameObject gameObject, Color color)
    {
        string key = GetObjectLineColorKey(gameObject);
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        EditorPrefs.SetString(key, ColorUtility.ToHtmlStringRGBA(color));
        TrackObjectColorKey(key);
    }

    private static void ClearObjectLineColor(GameObject gameObject)
    {
        string key = GetObjectLineColorKey(gameObject);
        if (!string.IsNullOrEmpty(key))
        {
            EditorPrefs.DeleteKey(key);
            ForgetObjectColorKey(key);
        }
    }

    private static void SetObjectNameColor(GameObject gameObject, Color color)
    {
        string key = GetObjectNameColorKey(gameObject);
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        EditorPrefs.SetString(key, ColorUtility.ToHtmlStringRGBA(color));
        TrackObjectColorKey(key);
    }

    private static void ClearObjectNameColor(GameObject gameObject)
    {
        string key = GetObjectNameColorKey(gameObject);
        if (!string.IsNullOrEmpty(key))
        {
            EditorPrefs.DeleteKey(key);
            ForgetObjectColorKey(key);
        }
    }

    private static void SetObjectIconName(GameObject gameObject, string iconName)
    {
        string key = GetObjectIconNameKey(gameObject);
        if (string.IsNullOrEmpty(key) || string.IsNullOrWhiteSpace(iconName))
        {
            return;
        }

        string trimmedIconName = iconName.Trim();
        EditorPrefs.SetString(key, trimmedIconName);
        TrackObjectColorKey(key);
        SelectedObjectIconName = trimmedIconName;
        ClearUnityObjectIcon(gameObject);
    }

    private static void ClearObjectIconName(GameObject gameObject)
    {
        string key = GetObjectIconNameKey(gameObject);
        if (!string.IsNullOrEmpty(key))
        {
            EditorPrefs.DeleteKey(key);
            ForgetObjectColorKey(key);
            ClearUnityObjectIcon(gameObject);
        }
    }

    private static void ClearUnityObjectIcon(GameObject gameObject)
    {
        if (gameObject == null || SetIconForObjectMethod == null)
        {
            return;
        }

        SetIconForObjectMethod.Invoke(null, new object[] { gameObject, null });
        EditorUtility.SetDirty(gameObject);
    }

    private static void ClearUnityObjectIconsForStoredHierarchyIcons()
    {
        GameObject[] gameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject gameObject in gameObjects)
        {
            if (TryGetObjectIconName(gameObject, out _))
            {
                ClearUnityObjectIcon(gameObject);
            }
        }
    }

    private static void ClearToolPreferenceKeys()
    {
        EditorPrefs.DeleteKey(EnabledKey);
        EditorPrefs.DeleteKey(ShowTransformKey);
        EditorPrefs.DeleteKey(ShowHierarchyLinesKey);
        EditorPrefs.DeleteKey(ShowObjectRowColorsKey);
        EditorPrefs.DeleteKey(UseCustomHierarchyLineColorKey);
        EditorPrefs.DeleteKey(HierarchyLineColorKey);
        EditorPrefs.DeleteKey(SelectedObjectRowColorKey);
        EditorPrefs.DeleteKey(SelectedObjectLineColorKey);
        EditorPrefs.DeleteKey(SelectedObjectNameColorKey);
        EditorPrefs.DeleteKey(SelectedObjectIconNameKey);
        EditorPrefs.DeleteKey(ObjectColorKeyIndexKey);
    }

    private static void ClearLoadedObjectColorKeys()
    {
        GameObject[] gameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject gameObject in gameObjects)
        {
            DeleteObjectColorKey(GetObjectRowColorKey(gameObject));
            DeleteObjectColorKey(GetObjectLineColorKey(gameObject));
            DeleteObjectColorKey(GetObjectNameColorKey(gameObject));
            ClearObjectIconName(gameObject);
        }
    }

    private static void ClearIndexedObjectColorKeys()
    {
        foreach (string key in GetTrackedObjectColorKeys())
        {
            DeleteObjectColorKey(key);
        }
    }

    internal static int ClearMissingStylePreferenceKeys()
    {
        HashSet<string> keys = GetTrackedObjectColorKeys();
        HashSet<string> retainedKeys = new HashSet<string>(keys);
        int removedCount = 0;

        foreach (string key in keys)
        {
            if (!TryGetGlobalObjectIdFromStyleKey(key, out GlobalObjectId globalObjectId))
            {
                DeleteObjectColorKey(key);
                retainedKeys.Remove(key);
                removedCount++;
                continue;
            }

            if (!IsObjectIdFromLoadedScene(globalObjectId))
            {
                continue;
            }

            if (GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalObjectId) != null)
            {
                continue;
            }

            DeleteObjectColorKey(key);
            retainedKeys.Remove(key);
            removedCount++;
        }

        if (removedCount > 0)
        {
            SaveTrackedObjectColorKeys(retainedKeys);
        }

        return removedCount;
    }

    private static void DeleteObjectColorKey(string key)
    {
        if (!string.IsNullOrEmpty(key))
        {
            EditorPrefs.DeleteKey(key);
        }
    }

    private static void TrackObjectColorKey(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        HashSet<string> keys = GetTrackedObjectColorKeys();
        if (keys.Add(key))
        {
            SaveTrackedObjectColorKeys(keys);
        }
    }

    private static void ForgetObjectColorKey(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        HashSet<string> keys = GetTrackedObjectColorKeys();
        if (keys.Remove(key))
        {
            SaveTrackedObjectColorKeys(keys);
        }
    }

    private static HashSet<string> GetTrackedObjectColorKeys()
    {
        HashSet<string> keys = new HashSet<string>();
        string storedKeys = EditorPrefs.GetString(ObjectColorKeyIndexKey, string.Empty);
        if (string.IsNullOrEmpty(storedKeys))
        {
            return keys;
        }

        string[] splitKeys = storedKeys.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string key in splitKeys)
        {
            keys.Add(key);
        }

        return keys;
    }

    private static void SaveTrackedObjectColorKeys(HashSet<string> keys)
    {
        if (keys.Count == 0)
        {
            EditorPrefs.DeleteKey(ObjectColorKeyIndexKey);
            return;
        }

        EditorPrefs.SetString(ObjectColorKeyIndexKey, string.Join("\n", keys));
    }

    private static bool TryGetGlobalObjectIdFromStyleKey(string key, out GlobalObjectId globalObjectId)
    {
        if (TryTrimStyleKeyPrefix(key, ObjectRowColorKeyPrefix, out string globalObjectIdText)
            || TryTrimStyleKeyPrefix(key, ObjectLineColorKeyPrefix, out globalObjectIdText)
            || TryTrimStyleKeyPrefix(key, ObjectNameColorKeyPrefix, out globalObjectIdText)
            || TryTrimStyleKeyPrefix(key, ObjectIconNameKeyPrefix, out globalObjectIdText))
        {
            return GlobalObjectId.TryParse(globalObjectIdText, out globalObjectId);
        }

        globalObjectId = default;
        return false;
    }

    private static bool TryTrimStyleKeyPrefix(string key, string prefix, out string value)
    {
        if (!string.IsNullOrEmpty(key) && key.StartsWith(prefix, StringComparison.Ordinal))
        {
            value = key.Substring(prefix.Length);
            return !string.IsNullOrEmpty(value);
        }

        value = string.Empty;
        return false;
    }

    private static bool IsObjectIdFromLoadedScene(GlobalObjectId globalObjectId)
    {
        string assetGuid = globalObjectId.assetGUID.ToString();
        if (string.IsNullOrEmpty(assetGuid))
        {
            return false;
        }

        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
        {
            UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            if (!scene.isLoaded || string.IsNullOrEmpty(scene.path))
            {
                continue;
            }

            if (string.Equals(assetGuid, AssetDatabase.AssetPathToGUID(scene.path), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void ClosePaletteWindows()
    {
        HierarchyComponentIconPaletteWindow[] windows = Resources.FindObjectsOfTypeAll<HierarchyComponentIconPaletteWindow>();
        foreach (HierarchyComponentIconPaletteWindow window in windows)
        {
            if (window != null)
            {
                window.Close();
            }
        }
    }

    private static void AddObjectAndChildren(GameObject gameObject, List<GameObject> gameObjects, HashSet<int> addedInstanceIds)
    {
        if (gameObject == null || !addedInstanceIds.Add(gameObject.GetInstanceID()))
        {
            return;
        }

        gameObjects.Add(gameObject);

        Transform transform = gameObject.transform;
        for (int i = 0; i < transform.childCount; i++)
        {
            AddObjectAndChildren(transform.GetChild(i).gameObject, gameObjects, addedInstanceIds);
        }
    }
}
#endif

