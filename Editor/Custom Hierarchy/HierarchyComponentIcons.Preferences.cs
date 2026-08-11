#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Module: project-local persistence and cleanup.
// This file stores per-object style values, tracks all generated setting keys, removes stale records,
// clears Unity object icons when needed, and provides helper routines for building or pruning stored keys.
// It intentionally keeps storage mechanics separate from drawing and user-interface code.
public static partial class HierarchyComponentIcons
{
    private const int ProjectSettingsVersion = 1;
    private const string ProjectSettingsAssetPath = "Assets/Editor/Custom Hierarchy/HierarchyComponentIconsSettings.json";

    private static ProjectSettingsData projectSettings;
    private static bool projectSettingsLoaded;
    private static bool projectSettingsDirty;

    [Serializable]
    private sealed class ProjectSettingsData
    {
        public int version = ProjectSettingsVersion;
        public List<ProjectStringEntry> strings = new List<ProjectStringEntry>();
        public List<ProjectBoolEntry> bools = new List<ProjectBoolEntry>();
        public List<ProjectIntEntry> ints = new List<ProjectIntEntry>();
    }

    [Serializable]
    private sealed class ProjectStringEntry
    {
        public string key;
        public string value;

        public ProjectStringEntry()
        {
        }

        public ProjectStringEntry(string key, string value)
        {
            this.key = key;
            this.value = value;
        }
    }

    [Serializable]
    private sealed class ProjectBoolEntry
    {
        public string key;
        public bool value;

        public ProjectBoolEntry()
        {
        }

        public ProjectBoolEntry(string key, bool value)
        {
            this.key = key;
            this.value = value;
        }
    }

    [Serializable]
    private sealed class ProjectIntEntry
    {
        public string key;
        public int value;

        public ProjectIntEntry()
        {
        }

        public ProjectIntEntry(string key, int value)
        {
            this.key = key;
            this.value = value;
        }
    }

    private static void SetObjectRowColor(GameObject gameObject, Color color)
    {
        string key = GetObjectRowColorKey(gameObject);
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        SetProjectString(key, ColorUtility.ToHtmlStringRGBA(color));
        TrackObjectColorKey(key);
    }

    private static void ClearObjectRowColor(GameObject gameObject)
    {
        string key = GetObjectRowColorKey(gameObject);
        if (!string.IsNullOrEmpty(key))
        {
            DeleteProjectKey(key);
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

        SetProjectString(key, ColorUtility.ToHtmlStringRGBA(color));
        TrackObjectColorKey(key);
    }

    private static void ClearObjectLineColor(GameObject gameObject)
    {
        string key = GetObjectLineColorKey(gameObject);
        if (!string.IsNullOrEmpty(key))
        {
            DeleteProjectKey(key);
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

        SetProjectString(key, ColorUtility.ToHtmlStringRGBA(color));
        TrackObjectColorKey(key);
    }

    private static void ClearObjectNameColor(GameObject gameObject)
    {
        string key = GetObjectNameColorKey(gameObject);
        if (!string.IsNullOrEmpty(key))
        {
            DeleteProjectKey(key);
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
        SetProjectString(key, trimmedIconName);
        TrackObjectColorKey(key);
        SelectedObjectIconName = trimmedIconName;
        ClearUnityObjectIcon(gameObject);
    }

    private static void ClearObjectIconName(GameObject gameObject)
    {
        string key = GetObjectIconNameKey(gameObject);
        if (!string.IsNullOrEmpty(key))
        {
            DeleteProjectKey(key);
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
        DeleteProjectKey(EnabledKey);
        DeleteProjectKey(ShowTransformKey);
        DeleteProjectKey(ShowHierarchyLinesKey);
        DeleteProjectKey(ShowObjectRowColorsKey);
        DeleteProjectKey(UseCustomHierarchyLineColorKey);
        DeleteProjectKey(HierarchyLineColorKey);
        DeleteProjectKey(UseCustomDefaultObjectColorsKey);
        DeleteProjectKey(SelectedObjectRowColorKey);
        DeleteProjectKey(SelectedObjectLineColorKey);
        DeleteProjectKey(SelectedObjectNameColorKey);
        DeleteProjectKey(SelectedObjectIconNameKey);
        DeleteProjectKey(LanguageKey);
        DeleteProjectKey(ObjectColorKeyIndexKey);
        InvalidateLanguageCache();
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
            DeleteProjectKey(key);
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
        string storedKeys = GetProjectString(ObjectColorKeyIndexKey, string.Empty);
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
            DeleteProjectKey(ObjectColorKeyIndexKey);
            return;
        }

        SetProjectString(ObjectColorKeyIndexKey, string.Join("\n", keys));
    }

    private static void ClearStoredIconReferencesToTokens(HashSet<string> iconTokens)
    {
        if (iconTokens == null || iconTokens.Count == 0)
        {
            return;
        }

        EnsureProjectSettingsLoaded();
        List<string> removedKeys = new List<string>();

        for (int i = projectSettings.strings.Count - 1; i >= 0; i--)
        {
            ProjectStringEntry entry = projectSettings.strings[i];
            if (entry == null
                || string.IsNullOrEmpty(entry.key)
                || !entry.key.StartsWith(ObjectIconNameKeyPrefix, StringComparison.Ordinal)
                || !iconTokens.Contains(entry.value))
            {
                continue;
            }

            removedKeys.Add(entry.key);
            projectSettings.strings.RemoveAt(i);
        }

        if (removedKeys.Count == 0)
        {
            return;
        }

        HashSet<string> trackedKeys = GetTrackedObjectColorKeys();
        foreach (string removedKey in removedKeys)
        {
            trackedKeys.Remove(removedKey);
        }

        SaveTrackedObjectColorKeys(trackedKeys);
        MarkProjectSettingsDirty();
    }

    private static bool GetProjectBool(string key, bool fallback)
    {
        EnsureProjectSettingsLoaded();

        foreach (ProjectBoolEntry entry in projectSettings.bools)
        {
            if (entry != null && string.Equals(entry.key, key, StringComparison.Ordinal))
            {
                return entry.value;
            }
        }

        return fallback;
    }

    private static void SetProjectBool(string key, bool value)
    {
        EnsureProjectSettingsLoaded();
        if (SetBoolEntry(projectSettings, key, value))
        {
            MarkProjectSettingsDirty();
        }
    }

    private static int GetProjectInt(string key, int fallback)
    {
        EnsureProjectSettingsLoaded();

        foreach (ProjectIntEntry entry in projectSettings.ints)
        {
            if (entry != null && string.Equals(entry.key, key, StringComparison.Ordinal))
            {
                return entry.value;
            }
        }

        return fallback;
    }

    private static void SetProjectInt(string key, int value)
    {
        EnsureProjectSettingsLoaded();
        if (SetIntEntry(projectSettings, key, value))
        {
            MarkProjectSettingsDirty();
        }
    }

    private static string GetProjectString(string key, string fallback)
    {
        EnsureProjectSettingsLoaded();

        foreach (ProjectStringEntry entry in projectSettings.strings)
        {
            if (entry != null && string.Equals(entry.key, key, StringComparison.Ordinal))
            {
                return entry.value ?? fallback;
            }
        }

        return fallback;
    }

    private static void SetProjectString(string key, string value)
    {
        EnsureProjectSettingsLoaded();
        if (SetStringEntry(projectSettings, key, value))
        {
            MarkProjectSettingsDirty();
        }
    }

    private static void DeleteProjectKey(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        EnsureProjectSettingsLoaded();
        bool changed = RemoveStringEntry(projectSettings, key)
            | RemoveBoolEntry(projectSettings, key)
            | RemoveIntEntry(projectSettings, key);

        if (changed)
        {
            MarkProjectSettingsDirty();
        }
    }

    private static void EnsureProjectSettingsLoaded()
    {
        if (projectSettingsLoaded)
        {
            return;
        }

        projectSettings = LoadProjectSettingsFromFile() ?? new ProjectSettingsData();
        NormalizeProjectSettingsData(projectSettings);
        projectSettingsLoaded = true;
    }

    private static ProjectSettingsData LoadProjectSettingsFromFile()
    {
        if (!File.Exists(ProjectSettingsAssetPath))
        {
            return null;
        }

        try
        {
            string json = File.ReadAllText(ProjectSettingsAssetPath);
            return string.IsNullOrWhiteSpace(json)
                ? null
                : JsonUtility.FromJson<ProjectSettingsData>(json);
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
        {
            Debug.LogWarning("Hierarchy Component Icons could not read project settings JSON: " + exception.Message);
            return null;
        }
    }

    private static void MarkProjectSettingsDirty()
    {
        projectSettingsDirty = true;
    }

    private static void SaveProjectSettingsIfDirty()
    {
        if (!projectSettingsLoaded || !projectSettingsDirty)
        {
            return;
        }

        SaveProjectSettingsData();
    }

    private static void SaveProjectSettingsOnSceneClosing(UnityEngine.SceneManagement.Scene scene, bool removingScene)
    {
        SaveProjectSettingsIfDirty();
    }

    private static void SaveProjectSettingsData()
    {
        NormalizeProjectSettingsData(projectSettings);

        try
        {
            string directoryPath = Path.GetDirectoryName(ProjectSettingsAssetPath);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            File.WriteAllText(ProjectSettingsAssetPath, JsonUtility.ToJson(projectSettings, true));
            projectSettingsDirty = false;
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
        {
            Debug.LogWarning("Hierarchy Component Icons could not save project settings JSON: " + exception.Message);
        }
    }

    private static void NormalizeProjectSettingsData(ProjectSettingsData data)
    {
        if (data == null)
        {
            projectSettings = new ProjectSettingsData();
            return;
        }

        data.version = ProjectSettingsVersion;
        data.strings ??= new List<ProjectStringEntry>();
        data.bools ??= new List<ProjectBoolEntry>();
        data.ints ??= new List<ProjectIntEntry>();
    }

    private static bool SetStringEntry(ProjectSettingsData data, string key, string value)
    {
        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

        value ??= string.Empty;
        bool changed = false;
        ProjectStringEntry matchedEntry = null;

        for (int i = data.strings.Count - 1; i >= 0; i--)
        {
            ProjectStringEntry entry = data.strings[i];
            if (entry == null || !string.Equals(entry.key, key, StringComparison.Ordinal))
            {
                continue;
            }

            if (matchedEntry == null)
            {
                matchedEntry = entry;
            }
            else
            {
                data.strings.RemoveAt(i);
                changed = true;
            }
        }

        if (matchedEntry == null)
        {
            data.strings.Add(new ProjectStringEntry(key, value));
            return true;
        }

        if (matchedEntry.value == value)
        {
            return changed;
        }

        matchedEntry.value = value;
        return true;
    }

    private static bool SetBoolEntry(ProjectSettingsData data, string key, bool value)
    {
        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

        bool changed = false;
        ProjectBoolEntry matchedEntry = null;

        for (int i = data.bools.Count - 1; i >= 0; i--)
        {
            ProjectBoolEntry entry = data.bools[i];
            if (entry == null || !string.Equals(entry.key, key, StringComparison.Ordinal))
            {
                continue;
            }

            if (matchedEntry == null)
            {
                matchedEntry = entry;
            }
            else
            {
                data.bools.RemoveAt(i);
                changed = true;
            }
        }

        if (matchedEntry == null)
        {
            data.bools.Add(new ProjectBoolEntry(key, value));
            return true;
        }

        if (matchedEntry.value == value)
        {
            return changed;
        }

        matchedEntry.value = value;
        return true;
    }

    private static bool SetIntEntry(ProjectSettingsData data, string key, int value)
    {
        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

        bool changed = false;
        ProjectIntEntry matchedEntry = null;

        for (int i = data.ints.Count - 1; i >= 0; i--)
        {
            ProjectIntEntry entry = data.ints[i];
            if (entry == null || !string.Equals(entry.key, key, StringComparison.Ordinal))
            {
                continue;
            }

            if (matchedEntry == null)
            {
                matchedEntry = entry;
            }
            else
            {
                data.ints.RemoveAt(i);
                changed = true;
            }
        }

        if (matchedEntry == null)
        {
            data.ints.Add(new ProjectIntEntry(key, value));
            return true;
        }

        if (matchedEntry.value == value)
        {
            return changed;
        }

        matchedEntry.value = value;
        return true;
    }

    private static bool RemoveStringEntry(ProjectSettingsData data, string key)
    {
        bool changed = false;
        for (int i = data.strings.Count - 1; i >= 0; i--)
        {
            ProjectStringEntry entry = data.strings[i];
            if (entry != null && string.Equals(entry.key, key, StringComparison.Ordinal))
            {
                data.strings.RemoveAt(i);
                changed = true;
            }
        }

        return changed;
    }

    private static bool RemoveBoolEntry(ProjectSettingsData data, string key)
    {
        bool changed = false;
        for (int i = data.bools.Count - 1; i >= 0; i--)
        {
            ProjectBoolEntry entry = data.bools[i];
            if (entry != null && string.Equals(entry.key, key, StringComparison.Ordinal))
            {
                data.bools.RemoveAt(i);
                changed = true;
            }
        }

        return changed;
    }

    private static bool RemoveIntEntry(ProjectSettingsData data, string key)
    {
        bool changed = false;
        for (int i = data.ints.Count - 1; i >= 0; i--)
        {
            ProjectIntEntry entry = data.ints[i];
            if (entry != null && string.Equals(entry.key, key, StringComparison.Ordinal))
            {
                data.ints.RemoveAt(i);
                changed = true;
            }
        }

        return changed;
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

