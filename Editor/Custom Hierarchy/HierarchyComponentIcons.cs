#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Module: Core entry point for the hierarchy customization tool.
// This file owns shared constants, project-backed display settings, color presets, icon name presets,
// and InitializeOnLoad registration for hierarchy row drawing. Behavior-heavy features live in the
// partial files beside this one so this file stays focused on configuration and bootstrapping.
/// <summary>
/// Draws the icons for components attached to each GameObject in the Hierarchy.
/// </summary>
[InitializeOnLoad]
public static partial class HierarchyComponentIcons
{
    private const string ToolsMenuRoot = "Tools/Hierarchy Component Icons/";
    private const string ToolsDisplayMenu = ToolsMenuRoot + "Display/";
    private const string ToolsPaletteMenu = ToolsMenuRoot + "Palette/";
    private const string ToolsEmergencyMenu = ToolsMenuRoot + "Emergency/";
    private const string GameObjectMenuRoot = "GameObject/Hierarchy Component Icons/";
    private const string GameObjectPaletteMenu = GameObjectMenuRoot + "Palette/";
    private const string GameObjectMainColorMenu = GameObjectMenuRoot + "Main Color/";
    private const string GameObjectLineColorMenu = GameObjectMenuRoot + "Line Color/";
    private const string EnabledKey = "HierarchyComponentIcons.Enabled";
    private const string ShowTransformKey = "HierarchyComponentIcons.ShowTransform";
    private const string ShowHierarchyLinesKey = "HierarchyComponentIcons.ShowHierarchyLines";
    private const string ShowObjectRowColorsKey = "HierarchyComponentIcons.ShowObjectRowColors";
    private const string UseCustomHierarchyLineColorKey = "HierarchyComponentIcons.UseCustomHierarchyLineColor";
    private const string HierarchyLineColorKey = "HierarchyComponentIcons.HierarchyLineColor";
    private const string UseCustomDefaultObjectColorsKey = "HierarchyComponentIcons.UseCustomDefaultObjectColors";
    private const string SelectedObjectRowColorKey = "HierarchyComponentIcons.SelectedObjectRowColor";
    private const string ObjectRowColorKeyPrefix = "HierarchyComponentIcons.ObjectRowColor.";
    private const string SelectedObjectLineColorKey = "HierarchyComponentIcons.SelectedObjectLineColor";
    private const string ObjectLineColorKeyPrefix = "HierarchyComponentIcons.ObjectLineColor.";
    private const string SelectedObjectNameColorKey = "HierarchyComponentIcons.SelectedObjectNameColor";
    private const string ObjectNameColorKeyPrefix = "HierarchyComponentIcons.ObjectNameColor.";
    private const string SelectedObjectIconNameKey = "HierarchyComponentIcons.SelectedObjectIconName";
    private const string ObjectIconNameKeyPrefix = "HierarchyComponentIcons.ObjectIconName.";
    private const string ObjectColorKeyIndexKey = "HierarchyComponentIcons.ObjectColorKeyIndex";
    private const string LanguageKey = "HierarchyComponentIcons.Language";
    private const string ComponentIconTokenPrefix = "component:";
    private const string CustomIconTokenPrefix = "custom:";
    private const string CustomIconImportFolder = "Assets/Editor/Custom Hierarchy/Custom Icons";
    private const float IconSize = 16f;
    private const float IconSpacing = 2f;
    private const float RightPadding = 4f;
    private const float NameSafetyWidth = 72f;
    private const float HierarchyIndentWidth = 14f;
    private const float ConnectorLineThickness = 1f;
    private const float ConnectorLaneOffset = 8f;
    private const float ConnectorHorizontalPadding = 12f;
    private const float RowColorStripeWidth = 4f;
    private const float RowColorOverlayAlpha = 0.18f;
    private const float BuiltInObjectIconWidth = 18f;
    private const float NameLabelBackgroundPadding = 2f;
    private const float NameLabelRightPadding = 4f;

    private static readonly Color[] RowColorPresets =
    {
        new Color(1f, 1f, 1f, 0.22f),
        new Color(0f, 0f, 0f, 0.2f),
        new Color(0.25f, 0.55f, 1f, 0.55f),
        new Color(0.2f, 0.75f, 0.95f, 0.5f),
        new Color(0.2f, 0.85f, 0.45f, 0.5f),
        new Color(0.55f, 0.9f, 0.25f, 0.48f),
        new Color(1f, 0.92f, 0.25f, 0.5f),
        new Color(1f, 0.72f, 0.25f, 0.55f),
        new Color(1f, 0.46f, 0.22f, 0.52f),
        new Color(1f, 0.28f, 0.28f, 0.5f),
        new Color(1f, 0.35f, 0.62f, 0.5f),
        new Color(0.82f, 0.38f, 1f, 0.48f),
        new Color(0.55f, 0.42f, 1f, 0.5f),
        new Color(0.62f, 0.62f, 0.62f, 0.35f),
        new Color(0.45f, 0.32f, 0.22f, 0.45f),
        new Color(0.98f, 0.7f, 0.86f, 0.48f),
        new Color(0.65f, 0.9f, 0.95f, 0.42f),
        new Color(0.85f, 0.85f, 0.55f, 0.42f)
    };

    private static readonly GUIContent IconContent = new GUIContent();
    private static readonly Dictionary<string, Texture> BuiltInIconCache = new Dictionary<string, Texture>();
    private static readonly System.Reflection.MethodInfo SetIconForObjectMethod = typeof(EditorGUIUtility).GetMethod(
        "SetIconForObject",
        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
        null,
        new[] { typeof(UnityEngine.Object), typeof(Texture2D) },
        null);
    private static Texture warningIcon;

    private static readonly string[] BuiltInObjectIconNames =
    {
        "GameObject Icon",
        "Prefab Icon",
        "PrefabModel Icon",
        "PrefabVariant Icon",
        "SceneAsset Icon",
        "Folder Icon",
        "Favorite Icon",
        "Camera Icon",
        "Light Icon",
        "DirectionalLight Icon",
        "PointLight Icon",
        "Spotlight Icon",
        "AreaLight Icon",
        "Transform Icon",
        "RectTransform Icon",
        "MeshRenderer Icon",
        "SkinnedMeshRenderer Icon",
        "MeshFilter Icon",
        "MeshCollider Icon",
        "BoxCollider Icon",
        "SphereCollider Icon",
        "CapsuleCollider Icon",
        "WheelCollider Icon",
        "TerrainCollider Icon",
        "Rigidbody Icon",
        "Rigidbody2D Icon",
        "ParticleSystem Icon",
        "TrailRenderer Icon",
        "LineRenderer Icon",
        "SpriteRenderer Icon",
        "Canvas Icon",
        "CanvasGroup Icon",
        "CanvasRenderer Icon",
        "EventSystem Icon",
        "AudioSource Icon",
        "AudioListener Icon",
        "AudioMixerGroup Icon",
        "Animator Icon",
        "Animation Icon",
        "Avatar Icon",
        "PlayableDirector Icon",
        "TimelineAsset Icon",
        "NavMeshAgent Icon",
        "NavMeshObstacle Icon",
        "Terrain Icon",
        "TreeEditor.Tree Icon",
        "WindZone Icon",
        "ReflectionProbe Icon",
        "LightProbeGroup Icon",
        "OcclusionArea Icon",
        "LODGroup Icon",
        "VideoPlayer Icon",
        "TextMesh Icon",
        "TextMeshPro Icon",
        "UI.Image Icon",
        "UI.Text Icon",
        "UI.Button Icon",
        "UI.Toggle Icon",
        "UI.Slider Icon",
        "UI.Scrollbar Icon",
        "UI.Dropdown Icon",
        "UI.InputField Icon",
        "GridLayoutGroup Icon",
        "HorizontalLayoutGroup Icon",
        "VerticalLayoutGroup Icon",
        "ScriptableObject Icon",
        "MonoScript Icon",
        "cs Script Icon",
        "Shader Icon",
        "Material Icon",
        "Texture Icon",
        "Texture2D Icon",
        "RenderTexture Icon",
        "Sprite Icon",
        "Cubemap Icon",
        "Font Icon",
        "GUISkin Icon",
        "PhysicMaterial Icon",
        "PhysicsMaterial2D Icon",
        "LightingDataAsset Icon",
        "BuildSettings.Editor.Small",
        "Settings Icon",
        "console.infoicon",
        "console.warnicon",
        "console.erroricon"
    };

    static HierarchyComponentIcons()
    {
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemGUI;
        EditorApplication.delayCall += ClearUnityObjectIconsForStoredHierarchyIcons;
        EditorApplication.projectChanged += InvalidateLanguageCache;
        EditorApplication.quitting += SaveProjectSettingsIfDirty;
        AssemblyReloadEvents.beforeAssemblyReload += SaveProjectSettingsIfDirty;
        UnityEditor.SceneManagement.EditorSceneManager.sceneClosing += SaveProjectSettingsOnSceneClosing;
    }

    private static bool Enabled
    {
        get => GetProjectBool(EnabledKey, true);
        set
        {
            SetProjectBool(EnabledKey, value);
            EditorApplication.RepaintHierarchyWindow();
        }
    }

    private static bool ShowTransform
    {
        get => GetProjectBool(ShowTransformKey, false);
        set
        {
            SetProjectBool(ShowTransformKey, value);
            EditorApplication.RepaintHierarchyWindow();
        }
    }

    internal static bool ShowHierarchyLines
    {
        get => GetProjectBool(ShowHierarchyLinesKey, true);
        set
        {
            SetProjectBool(ShowHierarchyLinesKey, value);
            EditorApplication.RepaintHierarchyWindow();
        }
    }

    internal static bool ShowObjectRowColors
    {
        get => GetProjectBool(ShowObjectRowColorsKey, true);
        set
        {
            SetProjectBool(ShowObjectRowColorsKey, value);
            EditorApplication.RepaintHierarchyWindow();
        }
    }

    internal static bool UseCustomHierarchyLineColor
    {
        get => GetProjectBool(UseCustomHierarchyLineColorKey, false);
        set
        {
            SetProjectBool(UseCustomHierarchyLineColorKey, value);
            EditorApplication.RepaintHierarchyWindow();
        }
    }

    internal static Color HierarchyLineColor
    {
        get => GetStoredColor(HierarchyLineColorKey, DefaultHierarchyLineColor);
        set
        {
            SetProjectString(HierarchyLineColorKey, ColorUtility.ToHtmlStringRGBA(value));
            EditorApplication.RepaintHierarchyWindow();
        }
    }

    internal static Color DefaultColor => Color.white;

    internal static Color DefaultHierarchyLineColor => DefaultColor;

    internal static bool UseCustomDefaultObjectColors
    {
        get => GetProjectBool(UseCustomDefaultObjectColorsKey, false);
        set
        {
            SetProjectBool(UseCustomDefaultObjectColorsKey, value);
            EditorApplication.RepaintHierarchyWindow();
        }
    }

    internal static Color CreateRandomDefaultColor()
    {
        Color color = Color.HSVToRGB(
            UnityEngine.Random.Range(0f, 1f),
            UnityEngine.Random.Range(0.55f, 0.9f),
            UnityEngine.Random.Range(0.75f, 1f));
        color.a = 1f;
        return color;
    }

    internal static Color ActiveHierarchyLineColor => UseCustomHierarchyLineColor
        ? HierarchyLineColor
        : DefaultHierarchyLineColor;

    internal static Color SelectedObjectRowColor
    {
        get => GetStoredColor(SelectedObjectRowColorKey, DefaultColor);
        set
        {
            SetProjectString(SelectedObjectRowColorKey, ColorUtility.ToHtmlStringRGBA(value));
            EditorApplication.RepaintHierarchyWindow();
        }
    }

    internal static Color SelectedObjectLineColor
    {
        get => GetStoredColor(SelectedObjectLineColorKey, DefaultColor);
        set
        {
            SetProjectString(SelectedObjectLineColorKey, ColorUtility.ToHtmlStringRGBA(value));
            EditorApplication.RepaintHierarchyWindow();
        }
    }

    internal static Color SelectedObjectNameColor
    {
        get => GetStoredColor(SelectedObjectNameColorKey, DefaultColor);
        set
        {
            SetProjectString(SelectedObjectNameColorKey, ColorUtility.ToHtmlStringRGBA(value));
            EditorApplication.RepaintHierarchyWindow();
        }
    }

    internal static Color[] PresetColors => RowColorPresets;

    internal static string SelectedObjectIconName
    {
        get => GetProjectString(SelectedObjectIconNameKey, "GameObject Icon");
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                DeleteProjectKey(SelectedObjectIconNameKey);
                return;
            }

            SetProjectString(SelectedObjectIconNameKey, value.Trim());
        }
    }

    internal static string[] BuiltInIconNames => BuiltInObjectIconNames;
}
#endif

