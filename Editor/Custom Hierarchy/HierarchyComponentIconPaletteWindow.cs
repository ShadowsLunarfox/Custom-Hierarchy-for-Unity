#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


// Module: Main hierarchy style management palette.
// This EditorWindow provides global line color controls, selected-object batch editing, a searchable list
// of changed objects, imported icon management, stale record cleanup, and scrollable UI for smaller screens.
// It calls the shared HierarchyComponentIcons APIs instead of reading or writing EditorPrefs directly.
internal sealed class HierarchyComponentIconPaletteWindow : EditorWindow
{
    private const int SwatchesPerRow = 9;
    private const float ImportedIconListHeight = 120f;
    private const float CustomizedObjectListHeight = 260f;
    private const float ContentMinWidth = 860f;
    private const float ParameterLabelWidth = 46f;

    private Vector2 windowScroll;
    private Vector2 importedIconScroll;
    private bool displaySettingsExpanded = true;
    private bool selectedObjectToolsExpanded = true;
    private bool importedIconsExpanded = true;
    private bool changedObjectsExpanded = true;
    private bool showSelectedOnly;
    private string customizedObjectSearch = string.Empty;
    private Vector2 customizedObjectScroll;
    private readonly HashSet<string> selectedImportedIconTokens = new HashSet<string>();

    internal static void Open()
    {
        HierarchyComponentIconPaletteWindow window = GetWindow<HierarchyComponentIconPaletteWindow>(HierarchyComponentIcons.T("WindowTitle"));
        window.minSize = new Vector2(760f, 420f);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        titleContent = new GUIContent(HierarchyComponentIcons.T("WindowTitle"));

        windowScroll = EditorGUILayout.BeginScrollView(
            windowScroll,
            true,
            true);

        using (new EditorGUILayout.VerticalScope(GUILayout.MinWidth(ContentMinWidth)))
        {
            EditorGUILayout.Space(8f);
            DrawSection(HierarchyComponentIcons.T("DisplaySettings"), ref displaySettingsExpanded, DrawParentLineColorControls);
            DrawSection(HierarchyComponentIcons.T("SelectedObjectTools"), ref selectedObjectToolsExpanded, DrawSelectedObjectTools);
            DrawSection(HierarchyComponentIcons.T("ImportedIconLibrary"), ref importedIconsExpanded, DrawImportedIconControls);
            DrawSection(HierarchyComponentIcons.T("ChangedObjectManager"), ref changedObjectsExpanded, DrawCustomizedObjectStyleControls);
        }

        EditorGUILayout.EndScrollView();
    }

    private static void DrawSection(string title, ref bool expanded, Action drawContent)
    {
        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
        {
            Rect headerRect = EditorGUILayout.GetControlRect(false, 22f);
            expanded = EditorGUI.Foldout(headerRect, expanded, title, true, EditorStyles.foldoutHeader);
            if (!expanded)
            {
                return;
            }

            EditorGUILayout.Space(4f);
            drawContent();
        }

        EditorGUILayout.Space(8f);
    }

    private static void DrawParentLineColorControls()
    {
        DrawLanguageControls();

        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField(HierarchyComponentIcons.T("HierarchyLines"), EditorStyles.boldLabel);
        EditorGUILayout.LabelField(HierarchyComponentIcons.T("HierarchyLinesHelp"), EditorStyles.miniLabel);

        EditorGUI.BeginChangeCheck();
        bool showHierarchyLines = EditorGUILayout.Toggle(HierarchyComponentIcons.T("ShowParentLines"), HierarchyComponentIcons.ShowHierarchyLines);
        if (EditorGUI.EndChangeCheck())
        {
            HierarchyComponentIcons.ShowHierarchyLines = showHierarchyLines;
        }

        EditorGUI.BeginChangeCheck();
        bool useCustomColor = EditorGUILayout.Toggle(HierarchyComponentIcons.T("UseCustomColor"), HierarchyComponentIcons.UseCustomHierarchyLineColor);
        if (EditorGUI.EndChangeCheck())
        {
            HierarchyComponentIcons.UseCustomHierarchyLineColor = useCustomColor;
        }

        DrawDisabledAwareBlock(HierarchyComponentIcons.UseCustomHierarchyLineColor, () =>
        {
            EditorGUI.BeginChangeCheck();
            Color selectedColor = EditorGUILayout.ColorField(
                new GUIContent(HierarchyComponentIcons.T("Color")),
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
        });

    }

    private static void DrawLanguageControls()
    {
        EditorGUILayout.LabelField(HierarchyComponentIcons.T("Language"), EditorStyles.boldLabel);
        EditorGUILayout.LabelField(HierarchyComponentIcons.T("LanguageHelp"), EditorStyles.miniLabel);

        EditorGUI.BeginChangeCheck();
        int selectedLanguage = EditorGUILayout.Popup(
            HierarchyComponentIcons.T("Language"),
            (int)HierarchyComponentIcons.SelectedLanguage,
            HierarchyComponentIcons.LanguageOptionLabels);

        if (EditorGUI.EndChangeCheck())
        {
            HierarchyComponentIcons.SelectedLanguage =
                (HierarchyComponentIcons.HierarchyComponentIconLanguage)selectedLanguage;
        }

        EditorGUILayout.LabelField(
            HierarchyComponentIcons.T("ActiveLanguage"),
            HierarchyComponentIcons.ActiveLanguageName,
            EditorStyles.miniLabel);
    }

    private void DrawSelectedObjectTools()
    {
        DrawToolSubsection("Alt Menu Defaults", "Controls the color values used when the Alt popup opens for objects without saved colors.", DrawDefaultObjectColorControls);
    }

    private static void DrawDefaultObjectColorControls()
    {
        EditorGUILayout.LabelField("Off: Alt menu uses random colors. On: Alt menu uses these three colors.", EditorStyles.miniLabel);

        EditorGUI.BeginChangeCheck();
        bool useCustomDefaults = EditorGUILayout.Toggle("Use Custom Default Colors", HierarchyComponentIcons.UseCustomDefaultObjectColors);
        if (EditorGUI.EndChangeCheck())
        {
            HierarchyComponentIcons.UseCustomDefaultObjectColors = useCustomDefaults;
        }

        DrawDisabledAwareBlock(HierarchyComponentIcons.UseCustomDefaultObjectColors, () =>
        {
            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawDefaultColorControl(
                    HierarchyComponentIcons.T("MainColor"),
                    HierarchyComponentIcons.SelectedObjectRowColor,
                    color => HierarchyComponentIcons.SelectedObjectRowColor = color);

                DrawDefaultColorControl(
                    HierarchyComponentIcons.T("NameColor"),
                    HierarchyComponentIcons.SelectedObjectNameColor,
                    color => HierarchyComponentIcons.SelectedObjectNameColor = color);

                DrawDefaultColorControl(
                    HierarchyComponentIcons.T("LineColor"),
                    HierarchyComponentIcons.SelectedObjectLineColor,
                    color => HierarchyComponentIcons.SelectedObjectLineColor = color);
            }

            EditorGUILayout.Space(4f);
            DrawPresetPalette(color =>
            {
                HierarchyComponentIcons.SelectedObjectRowColor = color;
                HierarchyComponentIcons.SelectedObjectNameColor = color;
                HierarchyComponentIcons.SelectedObjectLineColor = color;
            });
        });
    }

    private static void DrawDefaultColorControl(string label, Color color, Action<Color> setColor)
    {
        using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.MinWidth(180f)))
        {
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);

            EditorGUI.BeginChangeCheck();
            Color selectedColor = EditorGUILayout.ColorField(
                new GUIContent(HierarchyComponentIcons.T("Color")),
                color,
                true,
                true,
                false);

            if (EditorGUI.EndChangeCheck())
            {
                setColor(selectedColor);
            }

            if (GUILayout.Button(HierarchyComponentIcons.T("Default"), GUILayout.Width(70f)))
            {
                setColor(HierarchyComponentIcons.DefaultColor);
            }

            if (GUILayout.Button("Random", GUILayout.Width(70f)))
            {
                setColor(HierarchyComponentIcons.CreateRandomDefaultColor());
            }
        }
    }

    private static void DrawToolSubsection(string title, string helpText, Action drawContent)
    {
        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            if (!string.IsNullOrEmpty(helpText))
            {
                EditorGUILayout.LabelField(helpText, EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(4f);
            drawContent();
        }

        EditorGUILayout.Space(6f);
    }

    private static void DrawDisabledAwareBlock(bool enabled, Action drawContent)
    {
        Rect blockRect = EditorGUILayout.BeginVertical(GUI.skin.box);
        if (!enabled && Event.current.type == EventType.Repaint)
        {
            Color disabledColor = EditorGUIUtility.isProSkin
                ? new Color(0f, 0f, 0f, 0.22f)
                : new Color(0f, 0f, 0f, 0.08f);
            EditorGUI.DrawRect(blockRect, disabledColor);
        }

        using (new EditorGUI.DisabledScope(!enabled))
        {
            drawContent();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawImportedIconControls()
    {
        EditorGUILayout.LabelField(HierarchyComponentIcons.T("CustomIconAssets"), EditorStyles.boldLabel);
        EditorGUILayout.LabelField(HierarchyComponentIcons.T("CustomIconAssetsHelp"), EditorStyles.miniLabel);

        List<string> customIconTokens = HierarchyComponentIcons.GetImportedCustomIconTokens();
        PruneMissingSelectedImportedIcons(customIconTokens);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(HierarchyComponentIcons.T("ImportLocalIcon"), GUILayout.MinWidth(150f)))
            {
                ImportLocalIconFromPalette();
            }

            using (new EditorGUI.DisabledScope(selectedImportedIconTokens.Count == 0))
            {
                if (GUILayout.Button(HierarchyComponentIcons.T("DeleteSelected"), GUILayout.MinWidth(120f)))
                {
                    DeleteSelectedImportedIcons();
                }
            }
        }

        EditorGUILayout.LabelField(
            HierarchyComponentIcons.T("ImportedIconCount"),
            HierarchyComponentIcons.Format("ImportedIconCountValue", customIconTokens.Count, selectedImportedIconTokens.Count),
            EditorStyles.miniLabel);

        if (customIconTokens.Count == 0)
        {
            EditorGUILayout.HelpBox(HierarchyComponentIcons.T("NoCustomIcons"), MessageType.Info);
            return;
        }

        importedIconScroll = EditorGUILayout.BeginScrollView(importedIconScroll, GUILayout.Height(ImportedIconListHeight));
        DrawImportedIconGrid(customIconTokens);
        EditorGUILayout.EndScrollView();
    }

    private void ImportLocalIconFromPalette()
    {
        string sourcePath = EditorUtility.OpenFilePanel(
            HierarchyComponentIcons.T("ImportLocalIcon"),
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            string.Empty);

        if (string.IsNullOrEmpty(sourcePath))
        {
            return;
        }

        if (!HierarchyComponentIcons.TryImportCustomIconFromFile(sourcePath, out string importedIconToken, out string errorMessage))
        {
            EditorUtility.DisplayDialog(HierarchyComponentIcons.T("ImportLocalIcon"), errorMessage, HierarchyComponentIcons.T("OK"));
            return;
        }

        HierarchyComponentIcons.SelectedObjectIconName = importedIconToken;
        selectedImportedIconTokens.Clear();
        selectedImportedIconTokens.Add(importedIconToken);
    }

    private void DrawImportedIconGrid(List<string> customIconTokens)
    {
        int rowCount = 0;
        bool rowOpen = false;

        foreach (string customIconToken in customIconTokens)
        {
            Texture icon = HierarchyComponentIcons.GetObjectIconTexture(null, customIconToken);
            if (icon == null)
            {
                continue;
            }

            if (!rowOpen)
            {
                EditorGUILayout.BeginHorizontal();
                rowOpen = true;
            }

            DrawImportedIconButton(customIconToken, icon);

            rowCount++;
            if (rowCount >= SwatchesPerRow)
            {
                rowCount = 0;
                rowOpen = false;
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        }

        if (rowOpen)
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawImportedIconButton(string customIconToken, Texture icon)
    {
        bool selected = selectedImportedIconTokens.Contains(customIconToken);
        string displayName = HierarchyComponentIcons.GetIconDisplayName(customIconToken);
        GUIContent iconContent = new GUIContent(icon, displayName);

        Color previousBackgroundColor = GUI.backgroundColor;
        GUI.backgroundColor = selected ? new Color(0.35f, 0.65f, 1f, 1f) : previousBackgroundColor;
        bool changedSelection = GUILayout.Toggle(selected, iconContent, GUI.skin.button, GUILayout.Width(28f), GUILayout.Height(28f));
        GUI.backgroundColor = previousBackgroundColor;

        if (changedSelection != selected)
        {
            if (changedSelection)
            {
                selectedImportedIconTokens.Add(customIconToken);
            }
            else
            {
                selectedImportedIconTokens.Remove(customIconToken);
            }
        }
    }

    private void DeleteSelectedImportedIcons()
    {
        int selectedCount = selectedImportedIconTokens.Count;
        if (selectedCount == 0)
        {
            return;
        }

        if (!EditorUtility.DisplayDialog(
            HierarchyComponentIcons.T("DeleteImportedIcons"),
            HierarchyComponentIcons.Format("DeleteImportedIconsMessage", selectedCount),
            HierarchyComponentIcons.T("Delete"),
            HierarchyComponentIcons.T("Cancel")))
        {
            return;
        }

        List<string> iconTokens = new List<string>(selectedImportedIconTokens);
        HierarchyComponentIcons.ClearIconReferencesToTokens(iconTokens);
        int deletedCount = HierarchyComponentIcons.DeleteImportedCustomIcons(iconTokens);
        selectedImportedIconTokens.Clear();

        EditorUtility.DisplayDialog(
            HierarchyComponentIcons.T("DeleteImportedIcons"),
            HierarchyComponentIcons.Format("DeletedImportedIconsMessage", deletedCount),
            HierarchyComponentIcons.T("OK"));
    }

    private void PruneMissingSelectedImportedIcons(List<string> customIconTokens)
    {
        HashSet<string> availableIconTokens = new HashSet<string>(customIconTokens);
        selectedImportedIconTokens.RemoveWhere(iconToken => !availableIconTokens.Contains(iconToken));
    }

    private void DrawCustomizedObjectStyleControls()
    {
        EditorGUILayout.LabelField(HierarchyComponentIcons.T("ObjectsWithCustomStyle"), EditorStyles.boldLabel);
        EditorGUILayout.LabelField(HierarchyComponentIcons.T("ObjectsWithCustomStyleHelp"), EditorStyles.miniLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            showSelectedOnly = EditorGUILayout.Toggle(HierarchyComponentIcons.T("SelectedOnly"), showSelectedOnly);
            customizedObjectSearch = EditorGUILayout.TextField(HierarchyComponentIcons.T("Search"), customizedObjectSearch);

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(customizedObjectSearch)))
            {
                if (GUILayout.Button(HierarchyComponentIcons.T("ClearSearch"), GUILayout.MinWidth(100f)))
                {
                    customizedObjectSearch = string.Empty;
                    GUI.FocusControl(null);
                }
            }

            if (GUILayout.Button(HierarchyComponentIcons.T("Refresh"), GUILayout.MinWidth(80f)))
            {
                Repaint();
            }

            if (GUILayout.Button(HierarchyComponentIcons.T("CleanMissing"), GUILayout.MinWidth(104f)))
            {
                ShowCleanMissingResult();
            }
        }

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
            HierarchyComponentIcons.T("ObjectsWithCustomParameters"),
            styles.Count == totalStyleCount ? styles.Count.ToString() : styles.Count + " / " + totalStyleCount,
            EditorStyles.miniLabel);

        if (styles.Count == 0)
        {
            EditorGUILayout.HelpBox(HierarchyComponentIcons.T("NoMatchingObjects"), MessageType.Info);
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(HierarchyComponentIcons.T("ClearListed"), GUILayout.MinWidth(104f))
                && EditorUtility.DisplayDialog(
                    HierarchyComponentIcons.T("ClearListedObjects"),
                    HierarchyComponentIcons.T("ClearListedObjectsMessage"),
                    HierarchyComponentIcons.T("Clear"),
                    HierarchyComponentIcons.T("Cancel")))
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
            HierarchyComponentIcons.T("CleanMissing"),
            removedCount == 0
                ? HierarchyComponentIcons.T("NoMissingRecords")
                : HierarchyComponentIcons.Format("RemovedMissingRecords", removedCount),
            HierarchyComponentIcons.T("OK"));
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

    private static void DrawCustomizedObjectHeader()
    {
        GUIStyle headerLabelStyle = new GUIStyle(EditorStyles.miniBoldLabel)
        {
            alignment = TextAnchor.MiddleLeft
        };

        Rect headerRect = EditorGUILayout.GetControlRect(false, 24f);
        EditorGUI.DrawRect(
            headerRect,
            EditorGUIUtility.isProSkin ? new Color(0.23f, 0.23f, 0.23f, 1f) : new Color(0.78f, 0.78f, 0.78f, 1f));

        Rect objectRect = new Rect(headerRect.x + 6f, headerRect.y, 260f, headerRect.height);
        Rect parametersRect = new Rect(objectRect.xMax + 8f, headerRect.y, 260f, headerRect.height);
        Rect actionsRect = new Rect(headerRect.xMax - 216f, headerRect.y, 210f, headerRect.height);

        GUI.Label(objectRect, HierarchyComponentIcons.T("Object"), headerLabelStyle);
        GUI.Label(parametersRect, HierarchyComponentIcons.T("AppliedParameters"), headerLabelStyle);
        GUI.Label(actionsRect, HierarchyComponentIcons.T("Actions"), headerLabelStyle);
    }

    private void DrawCustomizedObjectRow(HierarchyComponentIcons.HierarchyObjectStyleInfo style)
    {
        GameObject gameObject = style.GameObject;
        if (gameObject == null)
        {
            return;
        }

        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.ObjectField(gameObject, typeof(GameObject), true, GUILayout.MinWidth(260f));
                GUILayout.Label(HierarchyComponentIcons.GetObjectHierarchyPath(gameObject), EditorStyles.miniLabel, GUILayout.MinWidth(260f));
                GUILayout.FlexibleSpace();
                DrawCustomizedObjectActions(gameObject);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(8f);
                GUILayout.Label(HierarchyComponentIcons.T("Status"), EditorStyles.miniBoldLabel, GUILayout.Width(56f));
                DrawStyleStatusLabels(style);
                GUILayout.FlexibleSpace();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(8f);
                DrawNamedColorParameterCell(
                    HierarchyComponentIcons.T("Main"),
                    gameObject,
                    style.HasRowColor,
                    style.RowColor,
                    HierarchyComponentIcons.SelectedObjectRowColor,
                    HierarchyComponentIcons.ApplyRowColorToObject,
                    HierarchyComponentIcons.ClearRowColorFromObject);

                DrawNamedColorParameterCell(
                    HierarchyComponentIcons.T("Line"),
                    gameObject,
                    style.HasLineColor,
                    style.LineColor,
                    HierarchyComponentIcons.SelectedObjectLineColor,
                    HierarchyComponentIcons.ApplyLineColorToObject,
                    HierarchyComponentIcons.ClearLineColorFromObject);

                DrawNamedColorParameterCell(
                    HierarchyComponentIcons.T("Name"),
                    gameObject,
                    style.HasNameColor,
                    style.NameColor,
                    HierarchyComponentIcons.SelectedObjectNameColor,
                    HierarchyComponentIcons.ApplyNameColorToObject,
                    HierarchyComponentIcons.ClearNameColorFromObject);

                GUILayout.Label(HierarchyComponentIcons.T("Icon"), EditorStyles.miniBoldLabel, GUILayout.Width(ParameterLabelWidth));
                DrawIconParameterCell(style);
                GUILayout.FlexibleSpace();
            }
        }
    }

    private static void DrawCustomizedObjectActions(GameObject gameObject)
    {
        if (GUILayout.Button(HierarchyComponentIcons.T("Select"), GUILayout.Width(54f)))
        {
            Selection.activeGameObject = gameObject;
            EditorGUIUtility.PingObject(gameObject);
        }

        if (GUILayout.Button(HierarchyComponentIcons.T("Edit"), GUILayout.Width(42f)))
        {
            Vector2 screenPosition = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
            HierarchyObjectColorPopupWindow.Open(gameObject, screenPosition);
        }

        if (GUILayout.Button(HierarchyComponentIcons.T("Clear"), GUILayout.Width(54f)))
        {
            HierarchyComponentIcons.ClearAllStylesFromObject(gameObject);
        }
    }

    private static void DrawStyleStatusLabels(HierarchyComponentIcons.HierarchyObjectStyleInfo style)
    {
        DrawStyleStatusLabel(HierarchyComponentIcons.T("Main"), style.HasRowColor);
        DrawStyleStatusLabel(HierarchyComponentIcons.T("Line"), style.HasLineColor);
        DrawStyleStatusLabel(HierarchyComponentIcons.T("Name"), style.HasNameColor);
        DrawStyleStatusLabel(HierarchyComponentIcons.T("Icon"), style.HasIcon);
    }

    private static void DrawStyleStatusLabel(string label, bool enabled)
    {
        Color previousColor = GUI.color;
        GUI.color = enabled ? Color.white : new Color(1f, 1f, 1f, 0.38f);
        GUILayout.Label(enabled ? label : "-", EditorStyles.miniButton, GUILayout.Width(42f));
        GUI.color = previousColor;
    }

    private static void DrawNamedColorParameterCell(
        string label,
        GameObject gameObject,
        bool hasColor,
        Color color,
        Color fallbackColor,
        Action<GameObject, Color> applyColor,
        Action<GameObject> clearColor)
    {
        GUILayout.Label(label, EditorStyles.miniBoldLabel, GUILayout.Width(ParameterLabelWidth));
        DrawColorParameterCell(gameObject, hasColor, color, fallbackColor, applyColor, clearColor);
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
            : HierarchyComponentIcons.T("Default");
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
        EditorGUILayout.LabelField(HierarchyComponentIcons.T("Presets"), EditorStyles.miniBoldLabel);

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
