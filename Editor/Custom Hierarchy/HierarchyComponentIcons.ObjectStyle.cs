#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Module: Per-object style operations.
// This file exposes the object-facing API for applying, clearing, and querying
// hierarchy style data such as main row color, connection line color, name color, and custom icon.
// It works with loaded GameObjects and delegates persistence details to HierarchyComponentIcons.Preferences.cs.
public static partial class HierarchyComponentIcons
{
    internal struct HierarchyObjectStyleInfo
    {
        internal GameObject GameObject;
        internal bool HasRowColor;
        internal Color RowColor;
        internal bool HasLineColor;
        internal Color LineColor;
        internal bool HasNameColor;
        internal Color NameColor;
        internal bool HasIcon;
        internal string IconName;
    }

    internal static void ApplySelectedRowColorToSelection()
    {
        ApplyRowColorToSelection(SelectedObjectRowColor);
    }

    internal static void ApplyRowColorToSelection(Color color)
    {
        ApplyRowColorToObjects(Selection.gameObjects, color);
    }

    internal static void ApplyRowColorToObject(GameObject gameObject, Color color)
    {
        SetObjectRowColor(gameObject, color);
        EditorApplication.RepaintHierarchyWindow();
    }

    internal static void ClearRowColorFromObject(GameObject gameObject)
    {
        ClearObjectRowColor(gameObject);
        EditorApplication.RepaintHierarchyWindow();
    }

    internal static void ClearSelectedObjectRowColors()
    {
        ClearRowColorFromObjects(Selection.gameObjects);
    }

    internal static void ApplySelectedLineColorToSelection()
    {
        ApplyLineColorToSelection(SelectedObjectLineColor);
    }

    internal static void ApplyLineColorToSelection(Color color)
    {
        ApplyLineColorToObjects(Selection.gameObjects, color);
    }

    internal static void ApplyLineColorToObject(GameObject gameObject, Color color)
    {
        SetObjectLineColor(gameObject, color);
        EditorApplication.RepaintHierarchyWindow();
    }

    internal static void ClearLineColorFromObject(GameObject gameObject)
    {
        ClearObjectLineColor(gameObject);
        EditorApplication.RepaintHierarchyWindow();
    }

    internal static void ClearSelectedObjectLineColors()
    {
        ClearLineColorFromObjects(Selection.gameObjects);
    }

    internal static void ApplyNameColorToObject(GameObject gameObject, Color color)
    {
        SetObjectNameColor(gameObject, color);
        EditorApplication.RepaintHierarchyWindow();
    }

    internal static void ClearNameColorFromObject(GameObject gameObject)
    {
        ClearObjectNameColor(gameObject);
        EditorApplication.RepaintHierarchyWindow();
    }

    internal static void ApplyIconToObject(GameObject gameObject, string iconName)
    {
        SetObjectIconName(gameObject, iconName);
        EditorApplication.RepaintHierarchyWindow();
    }

    internal static void ClearIconFromObject(GameObject gameObject)
    {
        ClearObjectIconName(gameObject);
        EditorApplication.RepaintHierarchyWindow();
    }

    internal static void ApplyIconToObjects(IEnumerable<GameObject> gameObjects, string iconName)
    {
        foreach (GameObject gameObject in gameObjects)
        {
            SetObjectIconName(gameObject, iconName);
        }

        EditorApplication.RepaintHierarchyWindow();
    }

    internal static void ClearIconFromObjects(IEnumerable<GameObject> gameObjects)
    {
        foreach (GameObject gameObject in gameObjects)
        {
            ClearObjectIconName(gameObject);
        }

        EditorApplication.RepaintHierarchyWindow();
    }

    internal static void ApplyNameColorToObjects(IEnumerable<GameObject> gameObjects, Color color)
    {
        foreach (GameObject gameObject in gameObjects)
        {
            SetObjectNameColor(gameObject, color);
        }

        EditorApplication.RepaintHierarchyWindow();
    }

    internal static void ClearNameColorFromObjects(IEnumerable<GameObject> gameObjects)
    {
        foreach (GameObject gameObject in gameObjects)
        {
            ClearObjectNameColor(gameObject);
        }

        EditorApplication.RepaintHierarchyWindow();
    }

    internal static void RenameObject(GameObject gameObject, string newName)
    {
        if (gameObject == null || string.IsNullOrWhiteSpace(newName) || gameObject.name == newName)
        {
            return;
        }

        Undo.RecordObject(gameObject, "Rename Hierarchy Object");
        gameObject.name = newName;
        EditorUtility.SetDirty(gameObject);
        EditorApplication.RepaintHierarchyWindow();
    }

    internal static void ApplyRowColorToObjects(IEnumerable<GameObject> gameObjects, Color color)
    {
        foreach (GameObject gameObject in gameObjects)
        {
            SetObjectRowColor(gameObject, color);
        }

        EditorApplication.RepaintHierarchyWindow();
    }

    internal static void ClearRowColorFromObjects(IEnumerable<GameObject> gameObjects)
    {
        foreach (GameObject gameObject in gameObjects)
        {
            ClearObjectRowColor(gameObject);
        }

        EditorApplication.RepaintHierarchyWindow();
    }

    internal static void ApplyLineColorToObjects(IEnumerable<GameObject> gameObjects, Color color)
    {
        foreach (GameObject gameObject in gameObjects)
        {
            SetObjectLineColor(gameObject, color);
        }

        EditorApplication.RepaintHierarchyWindow();
    }

    internal static void ClearLineColorFromObjects(IEnumerable<GameObject> gameObjects)
    {
        foreach (GameObject gameObject in gameObjects)
        {
            ClearObjectLineColor(gameObject);
        }

        EditorApplication.RepaintHierarchyWindow();
    }

    internal static void ClearAllStylesFromObject(GameObject gameObject)
    {
        ClearObjectRowColor(gameObject);
        ClearObjectLineColor(gameObject);
        ClearObjectNameColor(gameObject);
        ClearObjectIconName(gameObject);
        EditorApplication.RepaintHierarchyWindow();
    }

    internal static void ClearIconReferencesToTokens(IEnumerable<string> iconTokens)
    {
        HashSet<string> iconTokenSet = new HashSet<string>(iconTokens);
        if (iconTokenSet.Count == 0)
        {
            return;
        }

        if (iconTokenSet.Contains(SelectedObjectIconName))
        {
            SelectedObjectIconName = string.Empty;
        }

        GameObject[] gameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject gameObject in gameObjects)
        {
            if (gameObject == null)
            {
                continue;
            }

            if (TryGetObjectIconName(gameObject, out string iconName)
                && iconTokenSet.Contains(iconName))
            {
                ClearObjectIconName(gameObject);
            }
        }

        ClearStoredIconReferencesToTokens(iconTokenSet);
        EditorApplication.RepaintHierarchyWindow();
    }

    internal static List<HierarchyObjectStyleInfo> GetCustomizedObjectStyles()
    {
        List<HierarchyObjectStyleInfo> styles = new List<HierarchyObjectStyleInfo>();
        GameObject[] gameObjects = Resources.FindObjectsOfTypeAll<GameObject>();

        foreach (GameObject gameObject in gameObjects)
        {
            if (gameObject == null || !gameObject.scene.IsValid())
            {
                continue;
            }

            HierarchyObjectStyleInfo style = new HierarchyObjectStyleInfo
            {
                GameObject = gameObject,
                HasRowColor = TryGetObjectRowColor(gameObject, out Color rowColor),
                RowColor = rowColor,
                HasLineColor = TryGetObjectLineColor(gameObject, out Color lineColor),
                LineColor = lineColor,
                HasNameColor = TryGetObjectNameColor(gameObject, out Color nameColor),
                NameColor = nameColor,
                HasIcon = TryGetObjectIconName(gameObject, out string iconName),
                IconName = iconName
            };

            if (style.HasRowColor || style.HasLineColor || style.HasNameColor || style.HasIcon)
            {
                styles.Add(style);
            }
        }

        styles.Sort((left, right) => string.Compare(
            GetObjectHierarchyPath(left.GameObject),
            GetObjectHierarchyPath(right.GameObject),
            StringComparison.OrdinalIgnoreCase));

        return styles;
    }

    internal static string GetObjectHierarchyPath(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return string.Empty;
        }

        string path = gameObject.name;
        Transform parent = gameObject.transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return string.IsNullOrEmpty(gameObject.scene.name) ? path : gameObject.scene.name + "/" + path;
    }

    internal static List<GameObject> GetSelectedObjects(bool includeChildren)
    {
        if (!includeChildren)
        {
            return new List<GameObject>(Selection.gameObjects);
        }

        return GetSelectionAndChildren();
    }

    internal static List<GameObject> GetSelectionAndChildren()
    {
        List<GameObject> gameObjects = new List<GameObject>();
        HashSet<int> addedInstanceIds = new HashSet<int>();

        foreach (GameObject gameObject in Selection.gameObjects)
        {
            AddObjectAndChildren(gameObject, gameObjects, addedInstanceIds);
        }

        return gameObjects;
    }

    internal static List<GameObject> GetRelatedObjects(GameObject targetGameObject)
    {
        List<GameObject> gameObjects = new List<GameObject>();
        HashSet<int> addedInstanceIds = new HashSet<int>();

        if (targetGameObject == null)
        {
            return gameObjects;
        }

        if (Selection.gameObjects.Length > 1 && Array.IndexOf(Selection.gameObjects, targetGameObject) >= 0)
        {
            foreach (GameObject gameObject in Selection.gameObjects)
            {
                AddObjectAndChildren(gameObject, gameObjects, addedInstanceIds);
            }

            return gameObjects;
        }

        AddObjectAndChildren(targetGameObject, gameObjects, addedInstanceIds);
        return gameObjects;
    }

    internal static bool TryGetObjectRowColor(GameObject gameObject, out Color color)
    {
        string key = GetObjectRowColorKey(gameObject);
        if (string.IsNullOrEmpty(key))
        {
            color = default;
            return false;
        }

        string storedColor = GetProjectString(key, string.Empty);
        if (!string.IsNullOrEmpty(storedColor) && ColorUtility.TryParseHtmlString("#" + storedColor, out color))
        {
            return true;
        }

        color = default;
        return false;
    }

    internal static bool TryGetObjectLineColor(GameObject gameObject, out Color color)
    {
        string key = GetObjectLineColorKey(gameObject);
        if (string.IsNullOrEmpty(key))
        {
            color = default;
            return false;
        }

        string storedColor = GetProjectString(key, string.Empty);
        if (!string.IsNullOrEmpty(storedColor) && ColorUtility.TryParseHtmlString("#" + storedColor, out color))
        {
            return true;
        }

        color = default;
        return false;
    }

    internal static bool TryGetObjectNameColor(GameObject gameObject, out Color color)
    {
        string key = GetObjectNameColorKey(gameObject);
        if (string.IsNullOrEmpty(key))
        {
            color = default;
            return false;
        }

        string storedColor = GetProjectString(key, string.Empty);
        if (!string.IsNullOrEmpty(storedColor) && ColorUtility.TryParseHtmlString("#" + storedColor, out color))
        {
            return true;
        }

        color = default;
        return false;
    }

    internal static bool TryGetObjectIconName(GameObject gameObject, out string iconName)
    {
        string key = GetObjectIconNameKey(gameObject);
        if (string.IsNullOrEmpty(key))
        {
            iconName = string.Empty;
            return false;
        }

        iconName = GetProjectString(key, string.Empty);
        return !string.IsNullOrWhiteSpace(iconName);
    }
}
#endif

