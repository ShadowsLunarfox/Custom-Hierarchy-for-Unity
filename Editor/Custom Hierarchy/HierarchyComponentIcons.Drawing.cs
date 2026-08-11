#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Module: Hierarchy window rendering.
// This file handles per-row GUI drawing in Unity's Hierarchy window, including component icons,
// custom object icons, name color overlays, row color stripes, and parent-child connection lines.
// It also owns the geometry helpers used to position labels, icons, and connector lines.
public static partial class HierarchyComponentIcons
{
    private static void OnHierarchyWindowItemGUI(int instanceId, Rect selectionRect)
    {
        if (!Enabled || !ShouldHandleHierarchyEvent(Event.current.type))
        {
            return;
        }

        GameObject gameObject = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
        if (gameObject == null)
        {
            return;
        }

        if (TryShowObjectColorShortcutMenu(selectionRect, gameObject))
        {
            return;
        }

        if (ShowObjectRowColors && Event.current.type == EventType.Repaint)
        {
            DrawObjectRowColor(selectionRect, gameObject);
        }

        if (ShowHierarchyLines && Event.current.type == EventType.Repaint)
        {
            DrawHierarchyConnectionLines(selectionRect, gameObject.transform);
        }

        if (Event.current.type == EventType.Repaint)
        {
            DrawObjectIcon(selectionRect, gameObject);
            DrawObjectNameColor(selectionRect, gameObject);
        }

        Component[] components = gameObject.GetComponents<Component>();
        if (components == null || components.Length == 0)
        {
            return;
        }

        DrawComponentIcons(selectionRect, components);
    }

    private static bool ShouldHandleHierarchyEvent(EventType eventType)
    {
        return eventType == EventType.Repaint
            || eventType == EventType.MouseDown
            || eventType == EventType.KeyDown
            || eventType == EventType.ContextClick;
    }

    private static bool TryShowObjectColorShortcutMenu(Rect rowRect, GameObject gameObject)
    {
        Event currentEvent = Event.current;
        if (!rowRect.Contains(currentEvent.mousePosition) || !IsObjectColorShortcutEvent(currentEvent))
        {
            return false;
        }

        ShowObjectColorShortcutMenu(gameObject);
        currentEvent.Use();
        return true;
    }

    private static bool IsObjectColorShortcutEvent(Event currentEvent)
    {
        if (currentEvent.type == EventType.KeyDown)
        {
            return currentEvent.keyCode == KeyCode.LeftAlt || currentEvent.keyCode == KeyCode.RightAlt;
        }

        if (currentEvent.type == EventType.ContextClick)
        {
            return currentEvent.alt;
        }

        return currentEvent.type == EventType.MouseDown && currentEvent.alt && (currentEvent.button == 0 || currentEvent.button == 1);
    }

    private static void ShowObjectColorShortcutMenu(GameObject gameObject)
    {
        Vector2 screenPosition = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
        HierarchyObjectColorPopupWindow.Open(gameObject, screenPosition);
    }

    private static void DrawComponentIcons(Rect rowRect, Component[] components)
    {
        Rect iconRect = new Rect(
            rowRect.xMax - RightPadding - IconSize,
            rowRect.y + Mathf.Max(0f, (rowRect.height - IconSize) * 0.5f),
            IconSize,
            IconSize);

        float minimumX = rowRect.xMin + NameSafetyWidth;

        for (int i = components.Length - 1; i >= 0; i--)
        {
            Component component = components[i];
            Type componentType = component != null ? component.GetType() : null;

            if (!ShowTransform && component is Transform)
            {
                continue;
            }

            if (iconRect.xMin < minimumX)
            {
                break;
            }

            DrawIcon(iconRect, component, componentType);
            iconRect.x -= IconSize + IconSpacing;
        }
    }

    private static void DrawObjectRowColor(Rect rowRect, GameObject gameObject)
    {
        if (!TryGetObjectRowColor(gameObject, out Color color))
        {
            return;
        }

        Color overlayColor = new Color(color.r, color.g, color.b, Mathf.Min(color.a, RowColorOverlayAlpha));
        Rect overlayRect = new Rect(0f, rowRect.yMin, rowRect.xMax + RightPadding, rowRect.height);
        EditorGUI.DrawRect(overlayRect, overlayColor);

        Rect stripeRect = new Rect(0f, rowRect.yMin, RowColorStripeWidth, rowRect.height);
        EditorGUI.DrawRect(stripeRect, color);
    }

    private static void DrawObjectIcon(Rect rowRect, GameObject gameObject)
    {
        if (!TryGetObjectIconName(gameObject, out string iconName))
        {
            return;
        }

        Texture icon = GetObjectIconTexture(gameObject, iconName);
        if (icon == null)
        {
            return;
        }

        Rect iconBackgroundRect = new Rect(rowRect.xMin, rowRect.yMin, BuiltInObjectIconWidth, rowRect.height);
        DrawNameLabelBackground(iconBackgroundRect, rowRect, gameObject);

        Rect iconRect = new Rect(
            rowRect.xMin,
            rowRect.y + Mathf.Max(0f, (rowRect.height - IconSize) * 0.5f),
            IconSize,
            IconSize);
        GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
    }

    private static void DrawObjectNameColor(Rect rowRect, GameObject gameObject)
    {
        if (!TryGetObjectNameColor(gameObject, out Color color))
        {
            return;
        }

        GUIStyle labelStyle = new GUIStyle(EditorStyles.label)
        {
            normal =
            {
                textColor = color
            }
        };

        Rect labelRect = GetNameLabelRect(rowRect, labelStyle, gameObject.name);
        DrawNameLabelBackground(labelRect, rowRect, gameObject);
        EditorGUI.LabelField(labelRect, gameObject.name, labelStyle);
    }

    private static Rect GetNameLabelRect(Rect rowRect, GUIStyle labelStyle, string objectName)
    {
        float x = rowRect.xMin + BuiltInObjectIconWidth;
        float availableWidth = Mathf.Max(0f, rowRect.xMax - x - NameSafetyWidth - NameLabelRightPadding);
        float textWidth = labelStyle.CalcSize(new GUIContent(objectName)).x + NameLabelBackgroundPadding * 2f;
        float width = Mathf.Min(availableWidth, textWidth);
        return new Rect(x, rowRect.yMin, width, rowRect.height);
    }

    private static void DrawNameLabelBackground(Rect labelRect, Rect rowRect, GameObject gameObject)
    {
        EditorGUI.DrawRect(labelRect, GetHierarchyBackgroundColor(rowRect, gameObject));

        if (ShowObjectRowColors && TryGetObjectRowColor(gameObject, out Color rowColor))
        {
            Color overlayColor = new Color(rowColor.r, rowColor.g, rowColor.b, Mathf.Min(rowColor.a, RowColorOverlayAlpha));
            EditorGUI.DrawRect(labelRect, overlayColor);
        }
    }

    private static Color GetHierarchyBackgroundColor(Rect rowRect, GameObject gameObject)
    {
        if (Selection.Contains(gameObject))
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.17f, 0.36f, 0.53f, 1f)
                : new Color(0.24f, 0.48f, 0.90f, 1f);
        }

        if (Event.current != null && rowRect.Contains(Event.current.mousePosition))
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.27f, 0.27f, 0.27f, 1f)
                : new Color(0.69f, 0.69f, 0.69f, 1f);
        }

        return EditorGUIUtility.isProSkin
            ? new Color(0.219f, 0.219f, 0.219f, 1f)
            : new Color(0.76f, 0.76f, 0.76f, 1f);
    }

    private static void DrawHierarchyConnectionLines(Rect rowRect, Transform transform)
    {
        if (transform.parent == null)
        {
            return;
        }

        float rowMiddleY = Mathf.Round(rowRect.yMin + rowRect.height * 0.5f);
        float currentLaneX = Mathf.Round(rowRect.xMin - HierarchyIndentWidth - ConnectorLaneOffset * 0.5f);
        float horizontalEndX = Mathf.Round(rowRect.xMin - ConnectorHorizontalPadding);
        bool isLastChild = transform.GetSiblingIndex() >= transform.parent.childCount - 1;

        Color lineColor = GetActiveHierarchyLineColor(transform.gameObject);

        DrawLine(currentLaneX, rowRect.yMin, currentLaneX, isLastChild ? rowMiddleY : rowRect.yMax, lineColor);
        DrawLine(currentLaneX, rowMiddleY, horizontalEndX, rowMiddleY, lineColor);

        Transform ancestor = transform.parent;
        float ancestorLaneX = currentLaneX - HierarchyIndentWidth;

        while (ancestor.parent != null)
        {
            if (HasFollowingSibling(ancestor))
            {
                DrawLine(ancestorLaneX, rowRect.yMin, ancestorLaneX, rowRect.yMax, GetActiveHierarchyLineColor(ancestor.gameObject));
            }

            ancestor = ancestor.parent;
            ancestorLaneX -= HierarchyIndentWidth;
        }
    }

    private static bool HasFollowingSibling(Transform transform)
    {
        return transform.parent != null && transform.GetSiblingIndex() < transform.parent.childCount - 1;
    }

    private static Color GetActiveHierarchyLineColor(GameObject gameObject)
    {
        return TryGetObjectLineColor(gameObject, out Color color) ? color : ActiveHierarchyLineColor;
    }

    private static Color GetStoredColor(string key, Color fallback)
    {
        string storedColor = GetProjectString(key, string.Empty);
        if (!string.IsNullOrEmpty(storedColor) && ColorUtility.TryParseHtmlString("#" + storedColor, out Color color))
        {
            return color;
        }

        return fallback;
    }

    private static string GetObjectRowColorKey(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return string.Empty;
        }

        GlobalObjectId globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(gameObject);
        string globalId = globalObjectId.ToString();
        return string.IsNullOrEmpty(globalId) ? string.Empty : ObjectRowColorKeyPrefix + globalId;
    }

    private static string GetObjectLineColorKey(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return string.Empty;
        }

        GlobalObjectId globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(gameObject);
        string globalId = globalObjectId.ToString();
        return string.IsNullOrEmpty(globalId) ? string.Empty : ObjectLineColorKeyPrefix + globalId;
    }

    private static string GetObjectNameColorKey(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return string.Empty;
        }

        GlobalObjectId globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(gameObject);
        string globalId = globalObjectId.ToString();
        return string.IsNullOrEmpty(globalId) ? string.Empty : ObjectNameColorKeyPrefix + globalId;
    }

    private static string GetObjectIconNameKey(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return string.Empty;
        }

        GlobalObjectId globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(gameObject);
        string globalId = globalObjectId.ToString();
        return string.IsNullOrEmpty(globalId) ? string.Empty : ObjectIconNameKeyPrefix + globalId;
    }

    private static void DrawLine(float startX, float startY, float endX, float endY, Color color)
    {
        Rect lineRect;

        if (Mathf.Approximately(startX, endX))
        {
            lineRect = new Rect(
                startX,
                Mathf.Min(startY, endY),
                ConnectorLineThickness,
                Mathf.Abs(endY - startY));
        }
        else
        {
            lineRect = new Rect(
                Mathf.Min(startX, endX),
                startY,
                Mathf.Abs(endX - startX),
                ConnectorLineThickness);
        }

        EditorGUI.DrawRect(lineRect, color);
    }

    private static void DrawIcon(Rect iconRect, Component component, Type componentType)
    {
        Texture icon = GetComponentIcon(component, componentType);
        if (icon == null)
        {
            return;
        }

        IconContent.image = icon;
        string componentName = componentType != null ? ObjectNames.NicifyVariableName(componentType.Name) : "Missing script";
        IconContent.tooltip = componentName + " (Ctrl/Cmd-click to select component)";

        bool wasEnabled = GUI.enabled;
        Color previousColor = GUI.color;
        GUI.enabled = component != null;
        GUI.color = IsDisabled(component) ? new Color(1f, 1f, 1f, 0.38f) : previousColor;

        GUI.Label(iconRect, IconContent);

        GUI.color = previousColor;
        GUI.enabled = wasEnabled;

        Event currentEvent = Event.current;
        if (component != null
            && currentEvent.type == EventType.MouseDown
            && currentEvent.button == 0
            && (currentEvent.control || currentEvent.command)
            && iconRect.Contains(currentEvent.mousePosition))
        {
            Selection.activeObject = component;
            EditorGUIUtility.PingObject(component);
            currentEvent.Use();
        }
    }

    private static Texture GetComponentIcon(Component component, Type componentType)
    {
        if (component == null)
        {
            warningIcon ??= EditorGUIUtility.FindTexture("console.warnicon.sml") ?? EditorGUIUtility.FindTexture("console.warnicon");
            return warningIcon;
        }

        return EditorGUIUtility.ObjectContent(component, componentType).image;
    }

    private static bool IsDisabled(Component component)
    {
        if (component is Renderer renderer)
        {
            return !renderer.enabled;
        }

        if (component is Collider collider)
        {
            return !collider.enabled;
        }

        if (component is Behaviour behaviour)
        {
            return !behaviour.enabled;
        }

        return false;
    }
}
#endif

