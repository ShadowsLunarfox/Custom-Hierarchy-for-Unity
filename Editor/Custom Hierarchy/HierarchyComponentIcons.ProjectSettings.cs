#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Module: project-local JSON settings.
// This file owns the in-memory settings cache and writes it to the project only when Unity closes,
// a scene closes, or scripts reload.
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
}
#endif
