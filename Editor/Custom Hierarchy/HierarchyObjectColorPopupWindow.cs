#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


// Module: Quick per-object hierarchy style popup.
// This popup opens from hierarchy/object shortcuts and focuses on editing one target object at a time,
// with optional synchronization to related selected objects. It supports renaming, name color, main row color,
// connection line color, component icons, Unity built-in icons, and quick links back to the full palette.
internal sealed class HierarchyObjectColorPopupWindow : EditorWindow
{
    private enum PopupColorTarget
    {
        Name,
        Main,
        Line
    }

    private const float Width = 360f;
    private const float Height = 760f;
    private const float Padding = 10f;
    private const float SwatchSize = 16f;
    private const float ComponentIconGridHeight = 70f;
    private const float CustomIconGridHeight = 80f;
    private const float IconGridHeight = 110f;
    private const int SwatchesPerRow = 12;

    private static HierarchyObjectColorPopupWindow currentWindow;

    private GameObject targetGameObject;
    private List<GameObject> relatedObjects = new List<GameObject>();
    private string editedName;
    private string iconName;
    private string iconSearch = string.Empty;
    private Color nameColor;
    private Color mainColor;
    private Color lineColor;
    private bool syncLineToRelated;
    private bool syncStyleToRelated;
    private Vector2 contentScroll;
    private Vector2 componentIconScroll;
    private Vector2 customIconScroll;
    private Vector2 iconScroll;
    private bool watchingExternalFocus;

    internal static void Open(GameObject gameObject, Vector2 screenPosition)
    {
        if (currentWindow != null)
        {
            currentWindow.Close();
        }

        HierarchyObjectColorPopupWindow window = CreateInstance<HierarchyObjectColorPopupWindow>();
        window.targetGameObject = gameObject;
        window.relatedObjects = HierarchyComponentIcons.GetRelatedObjects(gameObject);
        window.syncLineToRelated = window.relatedObjects.Count > 1;
        window.syncStyleToRelated = false;
        window.editedName = gameObject.name;
        window.nameColor = HierarchyComponentIcons.TryGetObjectNameColor(gameObject, out Color textColor)
            ? textColor
            : HierarchyComponentIcons.SelectedObjectNameColor;
        window.mainColor = HierarchyComponentIcons.TryGetObjectRowColor(gameObject, out Color rowColor)
            ? rowColor
            : HierarchyComponentIcons.SelectedObjectRowColor;
        window.lineColor = HierarchyComponentIcons.TryGetObjectLineColor(gameObject, out Color connectorColor)
            ? connectorColor
            : HierarchyComponentIcons.SelectedObjectLineColor;
        window.iconName = HierarchyComponentIcons.TryGetObjectIconName(gameObject, out string storedIconName)
            ? storedIconName
            : HierarchyComponentIcons.SelectedObjectIconName;
        if (HierarchyComponentIcons.GetObjectIconTexture(gameObject, window.iconName) == null)
        {
            window.iconName = "GameObject Icon";
        }
        currentWindow = window;
        window.position = new Rect(screenPosition.x, screenPosition.y, Width, Height);
        window.ShowPopup();
        window.Focus();
    }

    internal static void CloseAllOpenWindows()
    {
        HierarchyObjectColorPopupWindow[] windows = Resources.FindObjectsOfTypeAll<HierarchyObjectColorPopupWindow>();
        foreach (HierarchyObjectColorPopupWindow window in windows)
        {
            if (window != null)
            {
                window.Close();
            }
        }

        currentWindow = null;
    }

    private void OnDisable()
    {
        StopWatchingExternalFocus();
        if (currentWindow == this)
        {
            currentWindow = null;
        }
    }

    private void OnLostFocus()
    {
        EditorApplication.delayCall -= CloseAfterLostFocusIfSafe;
        EditorApplication.delayCall += CloseAfterLostFocusIfSafe;
    }

    private void CloseAfterLostFocusIfSafe()
    {
        if (this == null || currentWindow != this)
        {
            return;
        }

        if (ShouldKeepOpenForFocusedWindow(EditorWindow.focusedWindow))
        {
            StartWatchingExternalFocus();
            return;
        }

        Close();
    }

    private void StartWatchingExternalFocus()
    {
        if (watchingExternalFocus)
        {
            return;
        }

        watchingExternalFocus = true;
        EditorApplication.update += CloseWhenExternalToolFinishes;
    }

    private void StopWatchingExternalFocus()
    {
        if (!watchingExternalFocus)
        {
            return;
        }

        watchingExternalFocus = false;
        EditorApplication.update -= CloseWhenExternalToolFinishes;
    }

    private void CloseWhenExternalToolFinishes()
    {
        if (this == null || currentWindow != this)
        {
            StopWatchingExternalFocus();
            return;
        }

        EditorWindow focusedWindow = EditorWindow.focusedWindow;
        if (focusedWindow == this)
        {
            StopWatchingExternalFocus();
            return;
        }

        if (ShouldKeepOpenForFocusedWindow(focusedWindow))
        {
            return;
        }

        StopWatchingExternalFocus();
        Close();
    }

    private static bool ShouldKeepOpenForFocusedWindow(EditorWindow focusedWindow)
    {
        if (focusedWindow == null)
        {
            return false;
        }

        if (focusedWindow is HierarchyObjectColorPopupWindow)
        {
            return true;
        }

        Type focusedType = focusedWindow.GetType();
        string focusedTypeName = focusedType.Name;
        string focusedFullName = focusedType.FullName ?? string.Empty;
        return focusedTypeName.IndexOf("ColorPicker", StringComparison.OrdinalIgnoreCase) >= 0
            || focusedFullName.IndexOf("ColorPicker", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void OnGUI()
    {
        if (targetGameObject == null)
        {
            Close();
            return;
        }

        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
        {
            Close();
            Event.current.Use();
            return;
        }

        DrawBackground();

        GUILayout.BeginArea(new Rect(Padding, Padding, Width - Padding * 2f, Height - Padding * 2f));
        contentScroll = EditorGUILayout.BeginScrollView(contentScroll);
        DrawHeader();
        GUILayout.Space(8f);
        DrawTargetScope();
        GUILayout.Space(6f);
        DrawModuleBox(DrawNameModule);
        DrawModuleBox(DrawIconModule);
        DrawModuleBox(() => DrawColorPanel("NameColor", PopupColorTarget.Name));
        DrawModuleBox(() => DrawColorPanel("MainColor", PopupColorTarget.Main));
        DrawModuleBox(() => DrawColorPanel("LineColor", PopupColorTarget.Line));
        EditorGUILayout.EndScrollView();
        DrawFooter();
        GUILayout.EndArea();
    }

    private static void DrawBackground()
    {
        Color backgroundColor = EditorGUIUtility.isProSkin
            ? new Color(0.15f, 0.15f, 0.15f, 0.98f)
            : new Color(0.94f, 0.94f, 0.94f, 0.98f);

        EditorGUI.DrawRect(new Rect(0f, 0f, Width, Height), backgroundColor);
    }

    private void DrawHeader()
    {
        EditorGUILayout.LabelField(ObjectNames.NicifyVariableName(targetGameObject.name), EditorStyles.boldLabel);
        int relatedCount = relatedObjects.Count;
        string targetText = relatedCount > 1
            ? HierarchyComponentIcons.Format("RelatedObjectsDetected", relatedCount)
            : HierarchyComponentIcons.T("ThisObjectOnly");
        EditorGUILayout.LabelField(targetText, EditorStyles.miniLabel);
    }

    private static void DrawModuleBox(Action drawContent)
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Space(2f);
        drawContent();
        GUILayout.Space(2f);
        GUILayout.EndVertical();
        GUILayout.Space(6f);
    }

    private void DrawNameModule()
    {
        EditorGUILayout.LabelField(HierarchyComponentIcons.T("Name"), EditorStyles.boldLabel);
        editedName = EditorGUILayout.TextField(HierarchyComponentIcons.T("ObjectName"), editedName);

        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(editedName) || targetGameObject.name == editedName))
        {
            if (GUILayout.Button(HierarchyComponentIcons.T("RenameObject")))
            {
                HierarchyComponentIcons.RenameObject(targetGameObject, editedName);
                Close();
            }
        }
    }

    private void DrawIconModule()
    {
        EditorGUILayout.LabelField(HierarchyComponentIcons.T("Icon"), EditorStyles.boldLabel);

        Texture previewIcon = HierarchyComponentIcons.GetObjectIconTexture(targetGameObject, iconName);
        using (new EditorGUILayout.HorizontalScope())
        {
            Rect previewRect = GUILayoutUtility.GetRect(24f, 24f, GUILayout.Width(24f), GUILayout.Height(24f));
            DrawIconPreview(previewRect, previewIcon);

            using (new EditorGUI.DisabledScope(previewIcon == null))
            {
                if (GUILayout.Button(HierarchyComponentIcons.T("ApplyIcon")))
                {
                    ApplyIcon(iconName);
                    Close();
                }
            }

            using (new EditorGUI.DisabledScope(!HasStoredIcon()))
            {
                if (GUILayout.Button(HierarchyComponentIcons.T("Clear"), GUILayout.Width(70f)))
                {
                    ClearIcon();
                    Close();
                }
            }
        }

        if (GUILayout.Button(HierarchyComponentIcons.T("ImportLocalIcon")))
        {
            ImportLocalIcon();
        }

        iconSearch = EditorGUILayout.TextField(HierarchyComponentIcons.T("Search"), iconSearch);
        DrawComponentIconGrid();
        DrawCustomIconGrid();
        DrawBuiltInIconGrid();
    }

    private void ImportLocalIcon()
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

        iconName = importedIconToken;
        ApplyIcon(importedIconToken);
        Close();
    }

    private void DrawColorPanel(string titleKey, PopupColorTarget colorTarget)
    {
        string title = HierarchyComponentIcons.T(titleKey);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        DrawStoredColorPreview(colorTarget);

        EditorGUI.BeginChangeCheck();
        Color editedColor = EditorGUILayout.ColorField(
            new GUIContent(HierarchyComponentIcons.T("Color")),
            GetEditedColor(colorTarget),
            true,
            true,
            false);

        if (EditorGUI.EndChangeCheck())
        {
            SetEditedColor(colorTarget, editedColor);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(HierarchyComponentIcons.Format("ApplyNamedValue", title)))
            {
                ApplyColor(colorTarget, GetEditedColor(colorTarget));
                Close();
            }

            using (new EditorGUI.DisabledScope(!HasStoredColor(colorTarget)))
            {
                if (GUILayout.Button(HierarchyComponentIcons.T("Clear"), GUILayout.Width(70f)))
                {
                    ClearColor(colorTarget);
                    Close();
                }
            }
        }

        DrawPresetSwatches(colorTarget);
    }

    private void DrawStoredColorPreview(PopupColorTarget colorTarget)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField(HierarchyComponentIcons.T("Current"), GUILayout.Width(64f));

            Rect colorRect = GUILayoutUtility.GetRect(42f, 18f, GUILayout.Width(42f), GUILayout.Height(18f));
            if (TryGetStoredColor(colorTarget, out Color color))
            {
                EditorGUI.DrawRect(colorRect, color);
            }
            else
            {
                EditorGUI.DrawRect(colorRect, EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.08f) : new Color(0f, 0f, 0f, 0.08f));
                GUI.Label(colorRect, "-", EditorStyles.centeredGreyMiniLabel);
            }

            GUILayout.FlexibleSpace();
        }
    }

    private void DrawBuiltInIconGrid()
    {
        EditorGUILayout.LabelField(HierarchyComponentIcons.T("UnityIcons"), EditorStyles.miniBoldLabel);
        iconScroll = EditorGUILayout.BeginScrollView(iconScroll, GUILayout.Height(IconGridHeight));

        int rowCount = 0;
        bool rowOpen = false;
        foreach (string builtInIconName in HierarchyComponentIcons.BuiltInIconNames)
        {
            if (!HierarchyComponentIcons.IsIconSearchMatch(builtInIconName, iconSearch))
            {
                continue;
            }

            Texture icon = HierarchyComponentIcons.GetBuiltInIcon(builtInIconName);
            if (icon == null)
            {
                continue;
            }

            if (!rowOpen)
            {
                EditorGUILayout.BeginHorizontal();
                rowOpen = true;
            }

            GUIContent iconContent = new GUIContent(icon, builtInIconName);
            if (GUILayout.Button(iconContent, GUILayout.Width(24f), GUILayout.Height(24f)))
            {
                iconName = builtInIconName;
                ApplyIcon(builtInIconName);
                Close();
            }

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

        if (!rowOpen && rowCount == 0)
        {
            GUILayout.Space(1f);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawCustomIconGrid()
    {
        List<string> customIconTokens = HierarchyComponentIcons.GetImportedCustomIconTokens();
        if (customIconTokens.Count == 0)
        {
            return;
        }

        EditorGUILayout.LabelField(HierarchyComponentIcons.T("ImportedIcons"), EditorStyles.miniBoldLabel);
        customIconScroll = EditorGUILayout.BeginScrollView(customIconScroll, GUILayout.Height(CustomIconGridHeight));

        int rowCount = 0;
        bool rowOpen = false;
        foreach (string customIconToken in customIconTokens)
        {
            if (!HierarchyComponentIcons.IsIconSearchMatch(customIconToken, iconSearch))
            {
                continue;
            }

            Texture icon = HierarchyComponentIcons.GetObjectIconTexture(targetGameObject, customIconToken);
            if (icon == null)
            {
                continue;
            }

            if (!rowOpen)
            {
                EditorGUILayout.BeginHorizontal();
                rowOpen = true;
            }

            string displayName = HierarchyComponentIcons.GetIconDisplayName(customIconToken);
            GUIContent iconContent = new GUIContent(icon, displayName);
            if (GUILayout.Button(iconContent, GUILayout.Width(24f), GUILayout.Height(24f)))
            {
                iconName = customIconToken;
                ApplyIcon(customIconToken);
                Close();
            }

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

        if (!rowOpen && rowCount == 0)
        {
            GUILayout.Space(1f);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawComponentIconGrid()
    {
        List<Component> components = GetComponentsWithIcons();
        if (components.Count == 0)
        {
            return;
        }

        EditorGUILayout.LabelField(HierarchyComponentIcons.T("ComponentIcons"), EditorStyles.miniBoldLabel);
        componentIconScroll = EditorGUILayout.BeginScrollView(componentIconScroll, GUILayout.Height(ComponentIconGridHeight));

        int rowCount = 0;
        bool rowOpen = false;
        foreach (Component component in components)
        {
            string componentIconToken = HierarchyComponentIcons.GetComponentIconToken(component);
            if (!HierarchyComponentIcons.IsIconSearchMatch(componentIconToken, iconSearch))
            {
                continue;
            }

            Type componentType = component.GetType();
            Texture icon = EditorGUIUtility.ObjectContent(component, componentType).image;
            if (icon == null)
            {
                continue;
            }

            if (!rowOpen)
            {
                EditorGUILayout.BeginHorizontal();
                rowOpen = true;
            }

            string displayName = ObjectNames.NicifyVariableName(componentType.Name);
            GUIContent iconContent = new GUIContent(icon, displayName);
            if (GUILayout.Button(iconContent, GUILayout.Width(24f), GUILayout.Height(24f)))
            {
                iconName = componentIconToken;
                ApplyIcon(componentIconToken);
                Close();
            }

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

        if (!rowOpen && rowCount == 0)
        {
            GUILayout.Space(1f);
        }

        EditorGUILayout.EndScrollView();
    }

    private List<Component> GetComponentsWithIcons()
    {
        List<Component> componentsWithIcons = new List<Component>();
        if (targetGameObject == null)
        {
            return componentsWithIcons;
        }

        HashSet<Type> listedTypes = new HashSet<Type>();
        Component[] components = targetGameObject.GetComponents<Component>();
        foreach (Component component in components)
        {
            if (component == null)
            {
                continue;
            }

            Type componentType = component.GetType();
            if (!listedTypes.Add(componentType))
            {
                continue;
            }

            if (EditorGUIUtility.ObjectContent(component, componentType).image != null)
            {
                componentsWithIcons.Add(component);
            }
        }

        return componentsWithIcons;
    }

    private static void DrawIconPreview(Rect iconRect, Texture icon)
    {
        EditorGUI.DrawRect(iconRect, EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.08f) : new Color(0f, 0f, 0f, 0.08f));
        if (icon == null)
        {
            GUI.Label(iconRect, "-", EditorStyles.centeredGreyMiniLabel);
            return;
        }

        GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
    }

    private void DrawTargetScope()
    {
        int relatedCount = relatedObjects.Count;
        using (new EditorGUI.DisabledScope(relatedCount <= 1))
        {
            syncLineToRelated = EditorGUILayout.Toggle(HierarchyComponentIcons.T("SyncLine"), syncLineToRelated);
            syncStyleToRelated = EditorGUILayout.Toggle(HierarchyComponentIcons.T("SyncColorsAndIcon"), syncStyleToRelated);
        }
    }

    private void DrawPresetSwatches(PopupColorTarget colorTarget)
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
                    Rect swatchRect = GUILayoutUtility.GetRect(SwatchSize, SwatchSize, GUILayout.Width(SwatchSize), GUILayout.Height(SwatchSize));
                    EditorGUI.DrawRect(swatchRect, color);

                    if (GUI.Button(swatchRect, GUIContent.none, GUIStyle.none))
                    {
                        SetEditedColor(colorTarget, color);
                        ApplyColor(colorTarget, color);
                        Close();
                    }
                }

                GUILayout.FlexibleSpace();
            }
        }
    }

    private Color GetEditedColor(PopupColorTarget colorTarget)
    {
        return colorTarget switch
        {
            PopupColorTarget.Name => nameColor,
            PopupColorTarget.Main => mainColor,
            _ => lineColor
        };
    }

    private void SetEditedColor(PopupColorTarget colorTarget, Color color)
    {
        switch (colorTarget)
        {
            case PopupColorTarget.Name:
                nameColor = color;
                break;
            case PopupColorTarget.Main:
                mainColor = color;
                break;
            case PopupColorTarget.Line:
                lineColor = color;
                break;
        }
    }

    private bool TryGetStoredColor(PopupColorTarget colorTarget, out Color color)
    {
        return colorTarget switch
        {
            PopupColorTarget.Name => HierarchyComponentIcons.TryGetObjectNameColor(targetGameObject, out color),
            PopupColorTarget.Main => HierarchyComponentIcons.TryGetObjectRowColor(targetGameObject, out color),
            _ => HierarchyComponentIcons.TryGetObjectLineColor(targetGameObject, out color)
        };
    }

    private bool HasStoredColor(PopupColorTarget colorTarget)
    {
        return TryGetStoredColor(colorTarget, out _);
    }

    private bool HasStoredIcon()
    {
        return HierarchyComponentIcons.TryGetObjectIconName(targetGameObject, out _);
    }

    private void ApplyIcon(string appliedIconName)
    {
        HierarchyComponentIcons.SelectedObjectIconName = appliedIconName;
        if (syncStyleToRelated && relatedObjects.Count > 1)
        {
            HierarchyComponentIcons.ApplyIconToObjects(relatedObjects, appliedIconName);
            return;
        }

        HierarchyComponentIcons.ApplyIconToObject(targetGameObject, appliedIconName);
    }

    private void ClearIcon()
    {
        if (syncStyleToRelated && relatedObjects.Count > 1)
        {
            HierarchyComponentIcons.ClearIconFromObjects(relatedObjects);
            return;
        }

        HierarchyComponentIcons.ClearIconFromObject(targetGameObject);
    }

    private void ApplyColor(PopupColorTarget colorTarget, Color color)
    {
        if (colorTarget == PopupColorTarget.Name)
        {
            HierarchyComponentIcons.SelectedObjectNameColor = color;
            if (syncStyleToRelated && relatedObjects.Count > 1)
            {
                HierarchyComponentIcons.ApplyNameColorToObjects(relatedObjects, color);
                return;
            }

            HierarchyComponentIcons.ApplyNameColorToObject(targetGameObject, color);
            return;
        }

        if (colorTarget == PopupColorTarget.Main)
        {
            HierarchyComponentIcons.SelectedObjectRowColor = color;
            if (syncStyleToRelated && relatedObjects.Count > 1)
            {
                HierarchyComponentIcons.ApplyRowColorToObjects(relatedObjects, color);
                return;
            }

            HierarchyComponentIcons.ApplyRowColorToObject(targetGameObject, color);
            return;
        }

        HierarchyComponentIcons.SelectedObjectLineColor = color;
        if (syncLineToRelated && relatedObjects.Count > 1)
        {
            HierarchyComponentIcons.ApplyLineColorToObjects(relatedObjects, color);
            return;
        }

        HierarchyComponentIcons.ApplyLineColorToObject(targetGameObject, color);
    }

    private void ClearColor(PopupColorTarget colorTarget)
    {
        if (colorTarget == PopupColorTarget.Name)
        {
            if (syncStyleToRelated && relatedObjects.Count > 1)
            {
                HierarchyComponentIcons.ClearNameColorFromObjects(relatedObjects);
                return;
            }

            HierarchyComponentIcons.ClearNameColorFromObject(targetGameObject);
            return;
        }

        if (colorTarget == PopupColorTarget.Main)
        {
            if (syncStyleToRelated && relatedObjects.Count > 1)
            {
                HierarchyComponentIcons.ClearRowColorFromObjects(relatedObjects);
                return;
            }

            HierarchyComponentIcons.ClearRowColorFromObject(targetGameObject);
            return;
        }

        if (syncLineToRelated && relatedObjects.Count > 1)
        {
            HierarchyComponentIcons.ClearLineColorFromObjects(relatedObjects);
            return;
        }

        HierarchyComponentIcons.ClearLineColorFromObject(targetGameObject);
    }

    private void DrawFooter()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(HierarchyComponentIcons.T("FullPalette")))
            {
                HierarchyComponentIconPaletteWindow.Open();
                Close();
            }

            if (GUILayout.Button(HierarchyComponentIcons.T("Close")))
            {
                Close();
            }
        }
    }
}
#endif
