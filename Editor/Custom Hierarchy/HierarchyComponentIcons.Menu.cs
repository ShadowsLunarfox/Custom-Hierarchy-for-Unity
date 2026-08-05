#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static partial class HierarchyComponentIcons
{
    [MenuItem(ToolsDisplayMenu + "Enabled")]
    private static void ToggleEnabled()
    {
        Enabled = !Enabled;
    }

    [MenuItem(ToolsDisplayMenu + "Enabled", true)]
    private static bool ToggleEnabledValidate()
    {
        Menu.SetChecked(ToolsDisplayMenu + "Enabled", Enabled);
        return true;
    }

    [MenuItem(ToolsDisplayMenu + "Show Transform Icon")]
    private static void ToggleShowTransform()
    {
        ShowTransform = !ShowTransform;
    }

    [MenuItem(ToolsDisplayMenu + "Show Transform Icon", true)]
    private static bool ToggleShowTransformValidate()
    {
        Menu.SetChecked(ToolsDisplayMenu + "Show Transform Icon", ShowTransform);
        return true;
    }

    [MenuItem(ToolsDisplayMenu + "Show Parent Lines")]
    private static void ToggleShowHierarchyLines()
    {
        ShowHierarchyLines = !ShowHierarchyLines;
    }

    [MenuItem(ToolsDisplayMenu + "Show Parent Lines", true)]
    private static bool ToggleShowHierarchyLinesValidate()
    {
        Menu.SetChecked(ToolsDisplayMenu + "Show Parent Lines", ShowHierarchyLines);
        return true;
    }

    [MenuItem(ToolsDisplayMenu + "Show Main Colors")]
    private static void ToggleShowObjectRowColors()
    {
        ShowObjectRowColors = !ShowObjectRowColors;
    }

    [MenuItem(ToolsDisplayMenu + "Show Main Colors", true)]
    private static bool ToggleShowObjectRowColorsValidate()
    {
        Menu.SetChecked(ToolsDisplayMenu + "Show Main Colors", ShowObjectRowColors);
        return true;
    }

    [MenuItem(ToolsPaletteMenu + "Open Color Palette")]
    private static void OpenColorPalette()
    {
        HierarchyComponentIconPaletteWindow.Open();
    }

    [MenuItem(ToolsEmergencyMenu + "Clear Blocking UI %#h")]
    private static void ClearBlockingUI()
    {
        HierarchyObjectColorPopupWindow.CloseAllOpenWindows();
        EditorApplication.RepaintHierarchyWindow();
    }

    [MenuItem(ToolsEmergencyMenu + "Reset All")]
    private static void ResetAll()
    {
        if (!EditorUtility.DisplayDialog(
                "Reset Hierarchy Component Icons",
                "Reset all settings, colors, object overrides, and close tool windows?",
                "Reset All",
                "Cancel"))
        {
            return;
        }

        HierarchyObjectColorPopupWindow.CloseAllOpenWindows();
        ClosePaletteWindows();
        ClearLoadedObjectColorKeys();
        ClearIndexedObjectColorKeys();
        ClearToolPreferenceKeys();
        EditorApplication.RepaintHierarchyWindow();
    }

    [MenuItem(GameObjectMainColorMenu + "Apply To Selection", false, 0)]
    private static void ApplyRowColorFromGameObjectMenu()
    {
        ApplySelectedRowColorToSelection();
    }

    [MenuItem(GameObjectMainColorMenu + "Clear Selection", false, 1)]
    private static void ClearRowColorFromGameObjectMenu()
    {
        ClearSelectedObjectRowColors();
    }

    [MenuItem(GameObjectPaletteMenu + "Open Color Palette", false, 30)]
    private static void OpenColorPaletteFromGameObjectMenu()
    {
        HierarchyComponentIconPaletteWindow.Open();
    }

    [MenuItem(GameObjectMainColorMenu + "Apply To Selection And Children", false, 2)]
    private static void ApplyRowColorToSelectionAndChildrenFromMenu()
    {
        ApplyRowColorToObjects(GetSelectionAndChildren(), SelectedObjectRowColor);
    }

    [MenuItem(GameObjectMainColorMenu + "Clear Selection And Children", false, 3)]
    private static void ClearRowColorFromSelectionAndChildrenFromMenu()
    {
        ClearRowColorFromObjects(GetSelectionAndChildren());
    }

    [MenuItem(GameObjectLineColorMenu + "Apply To Selection", false, 10)]
    private static void ApplyLineColorFromGameObjectMenu()
    {
        ApplySelectedLineColorToSelection();
    }

    [MenuItem(GameObjectLineColorMenu + "Clear Selection", false, 11)]
    private static void ClearLineColorFromGameObjectMenu()
    {
        ClearSelectedObjectLineColors();
    }

    [MenuItem(GameObjectLineColorMenu + "Apply To Selection And Children", false, 12)]
    private static void ApplyLineColorToSelectionAndChildrenFromMenu()
    {
        ApplyLineColorToObjects(GetSelectionAndChildren(), SelectedObjectLineColor);
    }

    [MenuItem(GameObjectLineColorMenu + "Clear Selection And Children", false, 13)]
    private static void ClearLineColorFromSelectionAndChildrenFromMenu()
    {
        ClearLineColorFromObjects(GetSelectionAndChildren());
    }
}
#endif

