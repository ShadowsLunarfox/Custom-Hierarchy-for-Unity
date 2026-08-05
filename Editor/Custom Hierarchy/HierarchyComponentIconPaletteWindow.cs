#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


internal sealed class HierarchyComponentIconPaletteWindow : EditorWindow
{
    private const int SwatchesPerRow = 9;
    private const float ObjectColorListHeight = 190f;
    private const float CustomizedObjectListHeight = 260f;
    private const float ContentMinWidth = 860f;

    private bool includeChildren = true;
    private Vector2 windowScroll;
    private Vector2 objectColorScroll;
    private bool showSelectedOnly;
    private bool pasteIncludeChildren = true;
    private bool clearMissingParametersOnPaste = true;
    private bool hasCopiedStyle;
    private string customizedObjectSearch = string.Empty;
    private string copiedStyleName = string.Empty;
    private Vector2 customizedObjectScroll;
    private HierarchyComponentIcons.HierarchyObjectStyleInfo copiedStyle;

    internal static void Open()
    {
        HierarchyComponentIconPaletteWindow window = GetWindow<HierarchyComponentIconPaletteWindow>("Hierarchy Palette");
        window.minSize = new Vector2(760f, 420f);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        windowScroll = EditorGUILayout.BeginScrollView(
            windowScroll,
            true,
            true);

        using (new EditorGUILayout.VerticalScope(GUILayout.MinWidth(ContentMinWidth)))
        {
            EditorGUILayout.Space(8f);
            DrawParentLineColorControls();
            EditorGUILayout.Space(12f);
            DrawObjectLineColorControls();
            EditorGUILayout.Space(12f);
            DrawCustomizedObjectStyleControls();
            EditorGUILayout.Space(12f);
        }

        EditorGUILayout.EndScrollView();
    }

    private static void DrawParentLineColorControls()
    {
        EditorGUILayout.LabelField("Parent Line Color", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        bool useCustomColor = EditorGUILayout.Toggle("Use Custom Color", HierarchyComponentIcons.UseCustomHierarchyLineColor);
        if (EditorGUI.EndChangeCheck())
        {
            HierarchyComponentIcons.UseCustomHierarchyLineColor = useCustomColor;
        }

        using (new EditorGUI.DisabledScope(!HierarchyComponentIcons.UseCustomHierarchyLineColor))
        {
            EditorGUI.BeginChangeCheck();
            Color selectedColor = EditorGUILayout.ColorField(
                new GUIContent("Color"),
                HierarchyComponentIcons.HierarchyLineColor,
                true,
                true,
                false);

            if (EditorGUI.EndChangeCheck())
            {
                HierarchyComponentIcons.HierarchyLineColor = selectedColor;
            }

            EditorGUILayout.Space(6f);
            DrawPresetPalette(color =>
            {
                HierarchyComponentIcons.HierarchyLineColor = color;
                HierarchyComponentIcons.UseCustomHierarchyLineColor = true;
            });
        }

        EditorGUILayout.Space(8f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Use Skin Default"))
            {
                HierarchyComponentIcons.UseCustomHierarchyLineColor = false;
            }

            if (GUILayout.Button("Reset Color"))
            {
                HierarchyComponentIcons.HierarchyLineColor = HierarchyComponentIcons.DefaultHierarchyLineColor;
            }
        }
    }

    private void DrawObjectLineColorControls()
    {
        EditorGUILayout.LabelField("Object Connection Line Color", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        bool showHierarchyLines = EditorGUILayout.Toggle("Show Parent Lines", HierarchyComponentIcons.ShowHierarchyLines);
        if (EditorGUI.EndChangeCheck())
        {
            HierarchyComponentIcons.ShowHierarchyLines = showHierarchyLines;
        }

        EditorGUI.BeginChangeCheck();
        Color selectedColor = EditorGUILayout.ColorField(
            new GUIContent("Color"),
            HierarchyComponentIcons.SelectedObjectLineColor,
            true,
            true,
            false);

        if (EditorGUI.EndChangeCheck())
        {
            HierarchyComponentIcons.SelectedObjectLineColor = selectedColor;
        }

        EditorGUILayout.Space(6f);
        DrawPresetPalette(color => HierarchyComponentIcons.SelectedObjectLineColor = color);

        EditorGUILayout.Space(8f);
        includeChildren = EditorGUILayout.Toggle("Include Children", includeChildren);

        List<GameObject> editableObjects = HierarchyComponentIcons.GetSelectedObjects(includeChildren);

        EditorGUILayout.Space(8f);
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(editableObjects.Count == 0))
            {
                if (GUILayout.Button("Apply To Listed"))
                {
                    HierarchyComponentIcons.ApplyLineColorToObjects(editableObjects, HierarchyComponentIcons.SelectedObjectLineColor);
                }

                if (GUILayout.Button("Clear Listed"))
                {
                    HierarchyComponentIcons.ClearLineColorFromObjects(editableObjects);
                }
            }
        }

        EditorGUILayout.LabelField("Selected Objects", Selection.gameObjects.Length.ToString(), EditorStyles.miniLabel);
        EditorGUILayout.LabelField("Editable Objects", editableObjects.Count.ToString(), EditorStyles.miniLabel);

        EditorGUILayout.Space(6f);
        DrawEditableObjectRows(editableObjects);
    }

    private void DrawEditableObjectRows(List<GameObject> editableObjects)
    {
        if (editableObjects.Count == 0)
        {
            EditorGUILayout.HelpBox("Select one or more GameObjects to edit their connection line colors.", MessageType.Info);
            return;
        }

        HashSet<int> listedInstanceIds = new HashSet<int>();
        foreach (GameObject gameObject in editableObjects)
        {
            listedInstanceIds.Add(gameObject.GetInstanceID());
        }

        objectColorScroll = EditorGUILayout.BeginScrollView(objectColorScroll, GUILayout.Height(ObjectColorListHeight));

        foreach (GameObject gameObject in editableObjects)
        {
            DrawEditableObjectRow(gameObject, listedInstanceIds);
        }

        EditorGUILayout.EndScrollView();
    }

    private static void DrawEditableObjectRow(GameObject gameObject, HashSet<int> listedInstanceIds)
    {
        bool hasColor = HierarchyComponentIcons.TryGetObjectLineColor(gameObject, out Color storedColor);
        Color lineColor = hasColor ? storedColor : HierarchyComponentIcons.SelectedObjectLineColor;
        int depth = GetListedParentDepth(gameObject.transform, listedInstanceIds);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(depth * 12f);
            EditorGUILayout.ObjectField(gameObject, typeof(GameObject), true, GUILayout.MinWidth(150f));

            EditorGUI.BeginChangeCheck();
            Color changedColor = EditorGUILayout.ColorField(GUIContent.none, lineColor, true, true, false, GUILayout.Width(92f));
            if (EditorGUI.EndChangeCheck())
            {
                HierarchyComponentIcons.ApplyLineColorToObject(gameObject, changedColor);
            }

            using (new EditorGUI.DisabledScope(!hasColor))
            {
                if (GUILayout.Button("Clear", GUILayout.Width(52f)))
                {
                    HierarchyComponentIcons.ClearLineColorFromObject(gameObject);
                }
            }
        }
    }

    private static int GetListedParentDepth(Transform transform, HashSet<int> listedInstanceIds)
    {
        int depth = 0;
        Transform parent = transform.parent;

        while (parent != null)
        {
            if (listedInstanceIds.Contains(parent.gameObject.GetInstanceID()))
            {
                depth++;
            }

            parent = parent.parent;
        }

        return depth;
    }

    private void DrawCustomizedObjectStyleControls()
    {
        EditorGUILayout.LabelField("Changed Objects", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            showSelectedOnly = EditorGUILayout.Toggle("Selected Only", showSelectedOnly);
            customizedObjectSearch = EditorGUILayout.TextField("Search", customizedObjectSearch);

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(customizedObjectSearch)))
            {
                if (GUILayout.Button("Clear Search", GUILayout.Width(92f)))
                {
                    customizedObjectSearch = string.Empty;
                    GUI.FocusControl(null);
                }
            }

            if (GUILayout.Button("Refresh", GUILayout.Width(80f)))
            {
                Repaint();
            }

            if (GUILayout.Button("Clean Missing", GUILayout.Width(100f)))
            {
                ShowCleanMissingResult();
            }
        }

        DrawCopiedStyleControls();

        List<HierarchyComponentIcons.HierarchyObjectStyleInfo> styles = HierarchyComponentIcons.GetCustomizedObjectStyles();
        int totalStyleCount = styles.Count;
        if (showSelectedOnly)
        {
            styles = FilterStylesBySelection(styles);
        }

        if (!string.IsNullOrWhiteSpace(customizedObjectSearch))
        {
            styles = FilterStylesBySearch(styles, customizedObjectSearch);
        }

        EditorGUILayout.LabelField(
            "Objects With Custom Parameters",
            styles.Count == totalStyleCount ? styles.Count.ToString() : styles.Count + " / " + totalStyleCount,
            EditorStyles.miniLabel);

        if (styles.Count == 0)
        {
            EditorGUILayout.HelpBox("No loaded GameObjects match the current custom style filters.", MessageType.Info);
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Clear Listed", GUILayout.Width(100f))
                && EditorUtility.DisplayDialog(
                    "Clear Listed Objects",
                    "Clear hierarchy colors and custom icons from the listed GameObjects?",
                    "Clear",
                    "Cancel"))
            {
                foreach (HierarchyComponentIcons.HierarchyObjectStyleInfo style in styles)
                {
                    HierarchyComponentIcons.ClearAllStylesFromObject(style.GameObject);
                }
            }
        }

        DrawCustomizedObjectHeader();

        customizedObjectScroll = EditorGUILayout.BeginScrollView(customizedObjectScroll, GUILayout.Height(CustomizedObjectListHeight));

        foreach (HierarchyComponentIcons.HierarchyObjectStyleInfo style in styles)
        {
            DrawCustomizedObjectRow(style);
        }

        EditorGUILayout.EndScrollView();
    }

    private static void ShowCleanMissingResult()
    {
        int removedCount = HierarchyComponentIcons.ClearMissingStylePreferenceKeys();
        EditorUtility.DisplayDialog(
            "Clean Missing",
            removedCount == 0
                ? "No missing custom style records were found for the loaded scenes."
                : "Removed " + removedCount + " missing custom style record(s).",
            "OK");
    }

    private static List<HierarchyComponentIcons.HierarchyObjectStyleInfo> FilterStylesBySelection(
        List<HierarchyComponentIcons.HierarchyObjectStyleInfo> styles)
    {
        HashSet<int> selectedInstanceIds = new HashSet<int>();
        foreach (GameObject gameObject in Selection.gameObjects)
        {
            if (gameObject != null)
            {
                selectedInstanceIds.Add(gameObject.GetInstanceID());
            }
        }

        List<HierarchyComponentIcons.HierarchyObjectStyleInfo> filteredStyles = new List<HierarchyComponentIcons.HierarchyObjectStyleInfo>();
        foreach (HierarchyComponentIcons.HierarchyObjectStyleInfo style in styles)
        {
            if (style.GameObject != null && selectedInstanceIds.Contains(style.GameObject.GetInstanceID()))
            {
                filteredStyles.Add(style);
            }
        }

        return filteredStyles;
    }

    private static List<HierarchyComponentIcons.HierarchyObjectStyleInfo> FilterStylesBySearch(
        List<HierarchyComponentIcons.HierarchyObjectStyleInfo> styles,
        string search)
    {
        List<HierarchyComponentIcons.HierarchyObjectStyleInfo> filteredStyles = new List<HierarchyComponentIcons.HierarchyObjectStyleInfo>();
        string normalizedSearch = search.Trim();

        foreach (HierarchyComponentIcons.HierarchyObjectStyleInfo style in styles)
        {
            if (IsStyleSearchMatch(style, normalizedSearch))
            {
                filteredStyles.Add(style);
            }
        }

        return filteredStyles;
    }

    private static bool IsStyleSearchMatch(HierarchyComponentIcons.HierarchyObjectStyleInfo style, string search)
    {
        if (string.IsNullOrEmpty(search))
        {
            return true;
        }

        GameObject gameObject = style.GameObject;
        if (gameObject == null)
        {
            return false;
        }

        return gameObject.name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
            || HierarchyComponentIcons.GetObjectHierarchyPath(gameObject).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
            || (style.HasIcon && HierarchyComponentIcons.GetIconDisplayName(style.IconName).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
            || (style.HasIcon && style.IconName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private void DrawCopiedStyleControls()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            pasteIncludeChildren = EditorGUILayout.Toggle("Paste Children", pasteIncludeChildren);
            clearMissingParametersOnPaste = EditorGUILayout.Toggle("Paste Exact Style", clearMissingParametersOnPaste);

            string copiedLabel = hasCopiedStyle ? "Copied: " + copiedStyleName : "Copied: None";
            GUILayout.Label(copiedLabel, EditorStyles.miniLabel, GUILayout.MinWidth(160f));

            using (new EditorGUI.DisabledScope(!hasCopiedStyle || Selection.gameObjects.Length == 0))
            {
                if (GUILayout.Button("Paste To Selection", GUILayout.Width(130f)))
                {
                    List<GameObject> targets = HierarchyComponentIcons.GetSelectedObjects(pasteIncludeChildren);
                    HierarchyComponentIcons.ApplyStyleToObjects(copiedStyle, targets, clearMissingParametersOnPaste);
                }
            }
        }
    }

    private static void DrawCustomizedObjectHeader()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("Object", EditorStyles.miniBoldLabel, GUILayout.MinWidth(220f));
            GUILayout.Label("Main", EditorStyles.miniBoldLabel, GUILayout.Width(98f));
            GUILayout.Label("Line", EditorStyles.miniBoldLabel, GUILayout.Width(98f));
            GUILayout.Label("Name", EditorStyles.miniBoldLabel, GUILayout.Width(98f));
            GUILayout.Label("Icon", EditorStyles.miniBoldLabel, GUILayout.Width(150f));
            GUILayout.Space(190f);
        }
    }

    private void DrawCustomizedObjectRow(HierarchyComponentIcons.HierarchyObjectStyleInfo style)
    {
        GameObject gameObject = style.GameObject;
        if (gameObject == null)
        {
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.ObjectField(gameObject, typeof(GameObject), true, GUILayout.MinWidth(220f));

            DrawColorParameterCell(
                gameObject,
                style.HasRowColor,
                style.RowColor,
                HierarchyComponentIcons.SelectedObjectRowColor,
                HierarchyComponentIcons.ApplyRowColorToObject,
                HierarchyComponentIcons.ClearRowColorFromObject);

            DrawColorParameterCell(
                gameObject,
                style.HasLineColor,
                style.LineColor,
                HierarchyComponentIcons.SelectedObjectLineColor,
                HierarchyComponentIcons.ApplyLineColorToObject,
                HierarchyComponentIcons.ClearLineColorFromObject);

            DrawColorParameterCell(
                gameObject,
                style.HasNameColor,
                style.NameColor,
                HierarchyComponentIcons.SelectedObjectNameColor,
                HierarchyComponentIcons.ApplyNameColorToObject,
                HierarchyComponentIcons.ClearNameColorFromObject);

            DrawIconParameterCell(style);

            if (GUILayout.Button("Copy", GUILayout.Width(46f)))
            {
                copiedStyle = style;
                copiedStyleName = gameObject.name;
                hasCopiedStyle = true;
            }

            if (GUILayout.Button("Select", GUILayout.Width(54f)))
            {
                Selection.activeGameObject = gameObject;
                EditorGUIUtility.PingObject(gameObject);
            }

            if (GUILayout.Button("Edit", GUILayout.Width(42f)))
            {
                Vector2 screenPosition = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
                HierarchyObjectColorPopupWindow.Open(gameObject, screenPosition);
            }

            if (GUILayout.Button("Clear", GUILayout.Width(46f)))
            {
                HierarchyComponentIcons.ClearAllStylesFromObject(gameObject);
            }
        }
    }

    private static void DrawColorParameterCell(
        GameObject gameObject,
        bool hasColor,
        Color color,
        Color fallbackColor,
        Action<GameObject, Color> applyColor,
        Action<GameObject> clearColor)
    {
        Color editedColor = hasColor ? color : fallbackColor;

        EditorGUI.BeginChangeCheck();
        Color changedColor = EditorGUILayout.ColorField(GUIContent.none, editedColor, true, true, false, GUILayout.Width(58f));
        if (EditorGUI.EndChangeCheck())
        {
            applyColor(gameObject, changedColor);
        }

        using (new EditorGUI.DisabledScope(!hasColor))
        {
            if (GUILayout.Button("X", GUILayout.Width(24f)))
            {
                clearColor(gameObject);
            }
        }

        GUILayout.Space(8f);
    }

    private static void DrawIconParameterCell(HierarchyComponentIcons.HierarchyObjectStyleInfo style)
    {
        Texture icon = style.HasIcon
            ? HierarchyComponentIcons.GetObjectIconTexture(style.GameObject, style.IconName)
            : null;

        Rect iconRect = GUILayoutUtility.GetRect(20f, 18f, GUILayout.Width(20f), GUILayout.Height(18f));
        if (icon != null)
        {
            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
        }

        string iconLabel = style.HasIcon
            ? HierarchyComponentIcons.GetIconDisplayName(style.IconName)
            : "Default";
        GUILayout.Label(iconLabel, EditorStyles.miniLabel, GUILayout.Width(86f));

        using (new EditorGUI.DisabledScope(!style.HasIcon))
        {
            if (GUILayout.Button("X", GUILayout.Width(24f)))
            {
                HierarchyComponentIcons.ClearIconFromObject(style.GameObject);
            }
        }

        GUILayout.Space(12f);
    }

    private static void DrawPresetPalette(Action<Color> applyColor)
    {
        EditorGUILayout.LabelField("Presets", EditorStyles.miniBoldLabel);

        Color[] colors = HierarchyComponentIcons.PresetColors;
        for (int i = 0; i < colors.Length; i += SwatchesPerRow)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                int rowEnd = Mathf.Min(i + SwatchesPerRow, colors.Length);
                for (int j = i; j < rowEnd; j++)
                {
                    Color color = colors[j];
                    Rect swatchRect = GUILayoutUtility.GetRect(26f, 22f, GUILayout.Width(26f), GUILayout.Height(22f));
                    EditorGUI.DrawRect(swatchRect, color);

                    if (GUI.Button(swatchRect, GUIContent.none, GUIStyle.none))
                    {
                        applyColor(color);
                    }
                }

                GUILayout.FlexibleSpace();
            }
        }
    }
}
#endif
