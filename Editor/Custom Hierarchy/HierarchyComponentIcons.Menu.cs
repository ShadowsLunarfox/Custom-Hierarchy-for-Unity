#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Module: Unity menu integration.
// This file registers the English-only Tools and GameObject menu entries, keeps menu checkmarks in sync
// with the current settings, opens management windows, and routes bulk color commands into the style API.
// Unity MenuItem paths must be compile-time constants, so runtime language switching is handled inside windows.
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
        return SetMenuChecked(ToolsDisplayMenu + "Enabled", Enabled);
    }

    [MenuItem(ToolsDisplayMenu + "Show Transform Icon")]
    private static void ToggleShowTransform()
    {
        ShowTransform = !ShowTransform;
    }

    [MenuItem(ToolsDisplayMenu + "Show Transform Icon", true)]
    private static bool ToggleShowTransformValidate()
    {
        return SetMenuChecked(ToolsDisplayMenu + "Show Transform Icon", ShowTransform);
    }

    [MenuItem(ToolsDisplayMenu + "Show Parent Lines")]
    private static void ToggleShowHierarchyLines()
    {
        ShowHierarchyLines = !ShowHierarchyLines;
    }

    [MenuItem(ToolsDisplayMenu + "Show Parent Lines", true)]
    private static bool ToggleShowHierarchyLinesValidate()
    {
        return SetMenuChecked(ToolsDisplayMenu + "Show Parent Lines", ShowHierarchyLines);
    }

    [MenuItem(ToolsDisplayMenu + "Show Main Colors")]
    private static void ToggleShowObjectRowColors()
    {
        ShowObjectRowColors = !ShowObjectRowColors;
    }

    [MenuItem(ToolsDisplayMenu + "Show Main Colors", true)]
    private static bool ToggleShowObjectRowColorsValidate()
    {
        return SetMenuChecked(ToolsDisplayMenu + "Show Main Colors", ShowObjectRowColors);
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
                T("ResetHierarchyComponentIcons"),
                T("ResetAllSettingsMessage"),
                T("ResetAll"),
                T("Cancel")))
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

    private static bool SetMenuChecked(string menuPath, bool checkedValue)
    {
        Menu.SetChecked(menuPath, checkedValue);
        return true;
    }
}
#endif
