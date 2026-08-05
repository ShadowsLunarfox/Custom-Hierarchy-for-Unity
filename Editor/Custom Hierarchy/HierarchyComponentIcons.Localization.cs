#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Module: Editor UI localization support.
// This file stores the selected editor language, detects the active language when Auto is enabled,
// and provides short translated UI strings used by the hierarchy customization windows.
public static partial class HierarchyComponentIcons
{
    private static bool languageCacheValid;
    private static HierarchyComponentIconLanguage cachedSelectedLanguage;
    private static HierarchyComponentIconLanguage cachedActiveLanguage;

    internal enum HierarchyComponentIconLanguage
    {
        Auto = 0,
        English = 1,
        SimplifiedChinese = 2,
        Korean = 3,
        Japanese = 4
    }

    internal static HierarchyComponentIconLanguage SelectedLanguage
    {
        get
        {
            EnsureLanguageCache();
            return cachedSelectedLanguage;
        }
        set
        {
            if (!Enum.IsDefined(typeof(HierarchyComponentIconLanguage), value))
            {
                value = HierarchyComponentIconLanguage.Auto;
            }

            EditorPrefs.SetInt(LanguageKey, (int)value);
            cachedSelectedLanguage = value;
            cachedActiveLanguage = value == HierarchyComponentIconLanguage.Auto
                ? DetectEditorLanguage()
                : value;
            languageCacheValid = true;
            EditorApplication.RepaintHierarchyWindow();
        }
    }

    internal static HierarchyComponentIconLanguage ActiveLanguage
    {
        get
        {
            EnsureLanguageCache();
            return cachedActiveLanguage;
        }
    }

    internal static string[] LanguageOptionLabels
    {
        get
        {
            return new[]
            {
                LanguageName(HierarchyComponentIconLanguage.Auto),
                LanguageName(HierarchyComponentIconLanguage.English),
                LanguageName(HierarchyComponentIconLanguage.SimplifiedChinese),
                LanguageName(HierarchyComponentIconLanguage.Korean),
                LanguageName(HierarchyComponentIconLanguage.Japanese)
            };
        }
    }

    internal static string ActiveLanguageName
    {
        get { return LanguageName(ActiveLanguage); }
    }

    internal static string LanguageName(HierarchyComponentIconLanguage language)
    {
        switch (language)
        {
            case HierarchyComponentIconLanguage.English:
                return "English";
            case HierarchyComponentIconLanguage.SimplifiedChinese:
                return "简体中文";
            case HierarchyComponentIconLanguage.Korean:
                return "한국어";
            case HierarchyComponentIconLanguage.Japanese:
                return "日本語";
            default:
                return "Auto";
        }
    }

    internal static string T(string key)
    {
        switch (ActiveLanguage)
        {
            case HierarchyComponentIconLanguage.SimplifiedChinese:
                return SimplifiedChineseText(key);
            case HierarchyComponentIconLanguage.Korean:
                return KoreanText(key);
            case HierarchyComponentIconLanguage.Japanese:
                return JapaneseText(key);
            default:
                return EnglishText(key);
        }
    }

    internal static string Format(string key, params object[] args)
    {
        return string.Format(T(key), args);
    }

    private static void EnsureLanguageCache()
    {
        if (languageCacheValid)
        {
            return;
        }

        cachedSelectedLanguage = ReadSelectedLanguageFromPrefs();
        cachedActiveLanguage = cachedSelectedLanguage == HierarchyComponentIconLanguage.Auto
            ? DetectEditorLanguage()
            : cachedSelectedLanguage;
        languageCacheValid = true;
    }

    private static HierarchyComponentIconLanguage ReadSelectedLanguageFromPrefs()
    {
        int storedValue = EditorPrefs.GetInt(LanguageKey, (int)HierarchyComponentIconLanguage.Auto);
        if (!Enum.IsDefined(typeof(HierarchyComponentIconLanguage), storedValue))
        {
            return HierarchyComponentIconLanguage.Auto;
        }

        return (HierarchyComponentIconLanguage)storedValue;
    }

    private static void InvalidateLanguageCache()
    {
        languageCacheValid = false;
        EditorApplication.RepaintHierarchyWindow();
    }

    private static HierarchyComponentIconLanguage DetectEditorLanguage()
    {
        if (TryDetectLanguageFromProjectSettings(out HierarchyComponentIconLanguage projectLanguage))
        {
            return projectLanguage;
        }

        if (TryDetectLanguageFromEditorPrefs(out HierarchyComponentIconLanguage editorLanguage))
        {
            return editorLanguage;
        }

        if (TryParseLanguageToken(Application.systemLanguage.ToString(), out HierarchyComponentIconLanguage systemLanguage))
        {
            return systemLanguage;
        }

        return HierarchyComponentIconLanguage.English;
    }

    private static bool TryDetectLanguageFromProjectSettings(out HierarchyComponentIconLanguage language)
    {
        language = HierarchyComponentIconLanguage.English;
        string[] projectSettingFiles =
        {
            "ProjectSettings/ProjectSettings.asset",
            "ProjectSettings/EditorSettings.asset"
        };

        foreach (string projectSettingFile in projectSettingFiles)
        {
            if (!File.Exists(projectSettingFile))
            {
                continue;
            }

            try
            {
                string[] lines = File.ReadAllLines(projectSettingFile);
                foreach (string line in lines)
                {
                    if (line.IndexOf("language", StringComparison.OrdinalIgnoreCase) < 0
                        && line.IndexOf("locale", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    if (TryParseLanguageToken(line, out language))
                    {
                        return true;
                    }
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        return false;
    }

    private static bool TryDetectLanguageFromEditorPrefs(out HierarchyComponentIconLanguage language)
    {
        language = HierarchyComponentIconLanguage.English;
        string[] editorPreferenceKeys =
        {
            "EditorLanguage",
            "UnityEditor.Language",
            "kEditorLanguage"
        };

        foreach (string editorPreferenceKey in editorPreferenceKeys)
        {
            string editorPreferenceValue = EditorPrefs.GetString(editorPreferenceKey, string.Empty);
            if (TryParseLanguageToken(editorPreferenceValue, out language))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryParseLanguageToken(string value, out HierarchyComponentIconLanguage language)
    {
        language = HierarchyComponentIconLanguage.English;
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        if (ContainsAny(value, "Chinese", "ChineseSimplified", "zh-", "zh_", ": zh", " zh", "简体", "中文"))
        {
            language = HierarchyComponentIconLanguage.SimplifiedChinese;
            return true;
        }

        if (ContainsAny(value, "Korean", "ko-", "ko_", ": ko", " ko", "한국어"))
        {
            language = HierarchyComponentIconLanguage.Korean;
            return true;
        }

        if (ContainsAny(value, "Japanese", "ja-", "ja_", ": ja", " ja", "日本語"))
        {
            language = HierarchyComponentIconLanguage.Japanese;
            return true;
        }

        if (ContainsAny(value, "English", "en-", "en_", ": en", " en"))
        {
            language = HierarchyComponentIconLanguage.English;
            return true;
        }

        return false;
    }

    private static bool ContainsAny(string value, params string[] tokens)
    {
        foreach (string token in tokens)
        {
            if (value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private static string EnglishText(string key)
    {
        switch (key)
        {
            case "WindowTitle": return "Hierarchy Palette";
            case "DisplaySettings": return "Display Settings";
            case "SelectedObjectTools": return "Selected Object Tools";
            case "ImportedIconLibrary": return "Imported Icon Library";
            case "ChangedObjectManager": return "Changed Object Manager";
            case "Language": return "Language";
            case "ActiveLanguage": return "Active Language";
            case "LanguageHelp": return "Auto checks project language settings first, then Unity editor/system language.";
            case "HierarchyLines": return "Hierarchy Lines";
            case "HierarchyLinesHelp": return "Control the global parent-child line display and fallback color.";
            case "UseCustomColor": return "Use Custom Color";
            case "Color": return "Color";
            case "UseSkinDefault": return "Use Skin Default";
            case "ResetColor": return "Reset Color";
            case "ResetHierarchyComponentIcons": return "Reset Hierarchy Component Icons";
            case "ResetAllSettingsMessage": return "Reset all settings, colors, object overrides, and close tool windows?";
            case "ResetAll": return "Reset All";
            case "SelectedObjectLineColor": return "Selected Object Line Color";
            case "SelectedObjectLineColorHelp": return "Apply or clear connection line colors for selected GameObjects.";
            case "ShowParentLines": return "Show Parent Lines";
            case "IncludeChildren": return "Include Children";
            case "ApplyToListed": return "Apply To Listed";
            case "ClearListed": return "Clear Listed";
            case "Scope": return "Scope";
            case "ScopeWithChildren": return "{0} editable objects from {1} selected roots";
            case "ScopeSelectedOnly": return "{0} selected objects";
            case "CustomIconAssets": return "Custom Icon Assets";
            case "CustomIconAssetsHelp": return "Import, select, or delete project-local custom hierarchy icons.";
            case "ImportLocalIcon": return "Import Local Icon";
            case "DeleteSelected": return "Delete Selected";
            case "ImportedIconCount": return "Imported Icon Count";
            case "ImportedIconCountValue": return "{0} ({1} selected)";
            case "NoCustomIcons": return "No custom icons have been imported yet.";
            case "DeleteImportedIcons": return "Delete Imported Icons";
            case "DeleteImportedIconsMessage": return "Delete {0} selected imported icon asset(s) from the project?";
            case "Delete": return "Delete";
            case "Cancel": return "Cancel";
            case "DeletedImportedIconsMessage": return "Deleted {0} imported icon asset(s).";
            case "OK": return "OK";
            case "ThisObjectOnly": return "This object only";
            case "RelatedObjectsDetected": return "{0} related objects detected";
            case "ObjectName": return "Object Name";
            case "RenameObject": return "Rename Object";
            case "ApplyIcon": return "Apply Icon";
            case "NameColor": return "Name Color";
            case "MainColor": return "Main Color";
            case "LineColor": return "Line Color";
            case "ApplyNamedValue": return "Apply {0}";
            case "Current": return "Current";
            case "UnityIcons": return "Unity Icons";
            case "ImportedIcons": return "Imported Icons";
            case "ComponentIcons": return "Component Icons";
            case "SyncLine": return "Sync Line";
            case "SyncColorsAndIcon": return "Sync Colors And Icon";
            case "FullPalette": return "Full Palette";
            case "Close": return "Close";
            case "ObjectsWithCustomStyle": return "Objects With Custom Style";
            case "ObjectsWithCustomStyleHelp": return "Review loaded objects that have custom hierarchy colors or icons.";
            case "SelectedOnly": return "Selected Only";
            case "Search": return "Search";
            case "ClearSearch": return "Clear Search";
            case "Refresh": return "Refresh";
            case "CleanMissing": return "Clean Missing";
            case "ObjectsWithCustomParameters": return "Objects With Custom Parameters";
            case "NoMatchingObjects": return "No loaded GameObjects match the current custom style filters.";
            case "ClearListedObjects": return "Clear Listed Objects";
            case "ClearListedObjectsMessage": return "Clear hierarchy colors and custom icons from the listed GameObjects?";
            case "Clear": return "Clear";
            case "NoMissingRecords": return "No missing custom style records were found for the loaded scenes.";
            case "RemovedMissingRecords": return "Removed {0} missing custom style record(s).";
            case "Object": return "Object";
            case "AppliedParameters": return "Applied Parameters";
            case "Actions": return "Actions";
            case "Status": return "Status";
            case "Main": return "Main";
            case "Line": return "Line";
            case "Name": return "Name";
            case "Icon": return "Icon";
            case "Select": return "Select";
            case "Edit": return "Edit";
            case "Default": return "Default";
            case "Presets": return "Presets";
            default: return key;
        }
    }

    private static string SimplifiedChineseText(string key)
    {
        switch (key)
        {
            case "WindowTitle": return "层级调色板";
            case "DisplaySettings": return "显示设置";
            case "SelectedObjectTools": return "选中物体工具";
            case "ImportedIconLibrary": return "导入图标库";
            case "ChangedObjectManager": return "已修改物体管理";
            case "Language": return "语言";
            case "ActiveLanguage": return "当前语言";
            case "LanguageHelp": return "自动模式会先检测工程语言设置，再跟随 Unity 编辑器或系统语言。";
            case "HierarchyLines": return "层级连接线";
            case "HierarchyLinesHelp": return "控制全局父子连接线显示和默认颜色。";
            case "UseCustomColor": return "使用自定义颜色";
            case "Color": return "颜色";
            case "UseSkinDefault": return "使用皮肤默认";
            case "ResetColor": return "重置颜色";
            case "ResetHierarchyComponentIcons": return "重置层级自定义";
            case "ResetAllSettingsMessage": return "要重置所有设置、颜色、物体覆盖项，并关闭工具窗口吗？";
            case "ResetAll": return "全部重置";
            case "SelectedObjectLineColor": return "选中物体连接线颜色";
            case "SelectedObjectLineColorHelp": return "为选中的 GameObject 应用或清除连接线颜色。";
            case "ShowParentLines": return "显示父级连接线";
            case "IncludeChildren": return "包含子物体";
            case "ApplyToListed": return "应用到列表";
            case "ClearListed": return "清除列表项";
            case "Scope": return "范围";
            case "ScopeWithChildren": return "{0} 个可编辑物体，来自 {1} 个选中根物体";
            case "ScopeSelectedOnly": return "{0} 个选中物体";
            case "CustomIconAssets": return "自定义图标资源";
            case "CustomIconAssetsHelp": return "导入、选择或删除工程内的自定义层级图标。";
            case "ImportLocalIcon": return "导入本地图标";
            case "DeleteSelected": return "删除选中";
            case "ImportedIconCount": return "已导入图标数量";
            case "ImportedIconCountValue": return "{0}（已选 {1}）";
            case "NoCustomIcons": return "还没有导入自定义图标。";
            case "DeleteImportedIcons": return "删除导入图标";
            case "DeleteImportedIconsMessage": return "要从工程中删除 {0} 个已选导入图标资源吗？";
            case "Delete": return "删除";
            case "Cancel": return "取消";
            case "DeletedImportedIconsMessage": return "已删除 {0} 个导入图标资源。";
            case "OK": return "确定";
            case "ThisObjectOnly": return "仅当前物体";
            case "RelatedObjectsDetected": return "检测到 {0} 个关联物体";
            case "ObjectName": return "物体名称";
            case "RenameObject": return "重命名物体";
            case "ApplyIcon": return "应用图标";
            case "NameColor": return "名称颜色";
            case "MainColor": return "主颜色";
            case "LineColor": return "连接线颜色";
            case "ApplyNamedValue": return "应用{0}";
            case "Current": return "当前";
            case "UnityIcons": return "Unity 图标";
            case "ImportedIcons": return "导入图标";
            case "ComponentIcons": return "组件图标";
            case "SyncLine": return "同步线条";
            case "SyncColorsAndIcon": return "同步颜色和图标";
            case "FullPalette": return "完整调色板";
            case "Close": return "关闭";
            case "ObjectsWithCustomStyle": return "带自定义样式的物体";
            case "ObjectsWithCustomStyleHelp": return "查看已加载物体上的层级颜色或图标设置。";
            case "SelectedOnly": return "仅选中";
            case "Search": return "搜索";
            case "ClearSearch": return "清空搜索";
            case "Refresh": return "刷新";
            case "CleanMissing": return "清理丢失";
            case "ObjectsWithCustomParameters": return "带自定义参数的物体";
            case "NoMatchingObjects": return "没有已加载的 GameObject 匹配当前筛选。";
            case "ClearListedObjects": return "清除列表物体";
            case "ClearListedObjectsMessage": return "要清除列表中 GameObject 的层级颜色和自定义图标吗？";
            case "Clear": return "清除";
            case "NoMissingRecords": return "当前已加载场景中没有丢失的样式记录。";
            case "RemovedMissingRecords": return "已移除 {0} 条丢失的样式记录。";
            case "Object": return "物体";
            case "AppliedParameters": return "已应用参数";
            case "Actions": return "操作";
            case "Status": return "状态";
            case "Main": return "主色";
            case "Line": return "线条";
            case "Name": return "名称";
            case "Icon": return "图标";
            case "Select": return "选择";
            case "Edit": return "编辑";
            case "Default": return "默认";
            case "Presets": return "预设";
            default: return EnglishText(key);
        }
    }

    private static string KoreanText(string key)
    {
        switch (key)
        {
            case "WindowTitle": return "Hierarchy 팔레트";
            case "DisplaySettings": return "표시 설정";
            case "SelectedObjectTools": return "선택 오브젝트 도구";
            case "ImportedIconLibrary": return "가져온 아이콘 라이브러리";
            case "ChangedObjectManager": return "변경된 오브젝트 관리";
            case "Language": return "언어";
            case "ActiveLanguage": return "현재 언어";
            case "LanguageHelp": return "자동 모드는 프로젝트 언어 설정을 먼저 확인한 뒤 Unity 에디터 또는 시스템 언어를 따릅니다.";
            case "HierarchyLines": return "Hierarchy 연결선";
            case "HierarchyLinesHelp": return "전역 부모-자식 연결선 표시와 기본 색상을 제어합니다.";
            case "UseCustomColor": return "사용자 색상 사용";
            case "Color": return "색상";
            case "UseSkinDefault": return "스킨 기본값 사용";
            case "ResetColor": return "색상 초기화";
            case "ResetHierarchyComponentIcons": return "Hierarchy 사용자 설정 초기화";
            case "ResetAllSettingsMessage": return "모든 설정, 색상, 오브젝트 재정의 값을 초기화하고 도구 창을 닫을까요?";
            case "ResetAll": return "전체 초기화";
            case "SelectedObjectLineColor": return "선택 오브젝트 연결선 색상";
            case "SelectedObjectLineColorHelp": return "선택한 GameObject의 연결선 색상을 적용하거나 지웁니다.";
            case "ShowParentLines": return "부모 연결선 표시";
            case "IncludeChildren": return "자식 포함";
            case "ApplyToListed": return "목록에 적용";
            case "ClearListed": return "목록 지우기";
            case "Scope": return "범위";
            case "ScopeWithChildren": return "선택한 루트 {1}개에서 편집 가능 오브젝트 {0}개";
            case "ScopeSelectedOnly": return "선택한 오브젝트 {0}개";
            case "CustomIconAssets": return "사용자 아이콘 에셋";
            case "CustomIconAssetsHelp": return "프로젝트 안의 사용자 Hierarchy 아이콘을 가져오거나 선택, 삭제합니다.";
            case "ImportLocalIcon": return "로컬 아이콘 가져오기";
            case "DeleteSelected": return "선택 삭제";
            case "ImportedIconCount": return "가져온 아이콘 수";
            case "ImportedIconCountValue": return "{0}개 ({1}개 선택됨)";
            case "NoCustomIcons": return "아직 가져온 사용자 아이콘이 없습니다.";
            case "DeleteImportedIcons": return "가져온 아이콘 삭제";
            case "DeleteImportedIconsMessage": return "선택한 가져온 아이콘 에셋 {0}개를 프로젝트에서 삭제할까요?";
            case "Delete": return "삭제";
            case "Cancel": return "취소";
            case "DeletedImportedIconsMessage": return "가져온 아이콘 에셋 {0}개를 삭제했습니다.";
            case "OK": return "확인";
            case "ThisObjectOnly": return "현재 오브젝트만";
            case "RelatedObjectsDetected": return "관련 오브젝트 {0}개 감지됨";
            case "ObjectName": return "오브젝트 이름";
            case "RenameObject": return "오브젝트 이름 변경";
            case "ApplyIcon": return "아이콘 적용";
            case "NameColor": return "이름 색상";
            case "MainColor": return "메인 색상";
            case "LineColor": return "연결선 색상";
            case "ApplyNamedValue": return "{0} 적용";
            case "Current": return "현재";
            case "UnityIcons": return "Unity 아이콘";
            case "ImportedIcons": return "가져온 아이콘";
            case "ComponentIcons": return "컴포넌트 아이콘";
            case "SyncLine": return "선 동기화";
            case "SyncColorsAndIcon": return "색상 및 아이콘 동기화";
            case "FullPalette": return "전체 팔레트";
            case "Close": return "닫기";
            case "ObjectsWithCustomStyle": return "사용자 스타일이 있는 오브젝트";
            case "ObjectsWithCustomStyleHelp": return "로드된 오브젝트의 Hierarchy 색상 또는 아이콘 설정을 확인합니다.";
            case "SelectedOnly": return "선택만";
            case "Search": return "검색";
            case "ClearSearch": return "검색 지우기";
            case "Refresh": return "새로고침";
            case "CleanMissing": return "누락 정리";
            case "ObjectsWithCustomParameters": return "사용자 파라미터가 있는 오브젝트";
            case "NoMatchingObjects": return "현재 스타일 필터와 일치하는 로드된 GameObject가 없습니다.";
            case "ClearListedObjects": return "목록 오브젝트 지우기";
            case "ClearListedObjectsMessage": return "목록의 GameObject에서 Hierarchy 색상과 사용자 아이콘을 지울까요?";
            case "Clear": return "지우기";
            case "NoMissingRecords": return "로드된 씬에서 누락된 스타일 기록을 찾지 못했습니다.";
            case "RemovedMissingRecords": return "누락된 스타일 기록 {0}개를 제거했습니다.";
            case "Object": return "오브젝트";
            case "AppliedParameters": return "적용된 파라미터";
            case "Actions": return "작업";
            case "Status": return "상태";
            case "Main": return "메인";
            case "Line": return "선";
            case "Name": return "이름";
            case "Icon": return "아이콘";
            case "Select": return "선택";
            case "Edit": return "편집";
            case "Default": return "기본값";
            case "Presets": return "프리셋";
            default: return EnglishText(key);
        }
    }

    private static string JapaneseText(string key)
    {
        switch (key)
        {
            case "WindowTitle": return "Hierarchy パレット";
            case "DisplaySettings": return "表示設定";
            case "SelectedObjectTools": return "選択オブジェクトツール";
            case "ImportedIconLibrary": return "インポート済みアイコン";
            case "ChangedObjectManager": return "変更済みオブジェクト管理";
            case "Language": return "言語";
            case "ActiveLanguage": return "現在の言語";
            case "LanguageHelp": return "自動はプロジェクトの言語設定を優先し、その後 Unity エディターまたはシステム言語に従います。";
            case "HierarchyLines": return "Hierarchy 接続線";
            case "HierarchyLinesHelp": return "全体の親子接続線表示と既定色を制御します。";
            case "UseCustomColor": return "カスタム色を使用";
            case "Color": return "色";
            case "UseSkinDefault": return "スキン既定値を使用";
            case "ResetColor": return "色をリセット";
            case "ResetHierarchyComponentIcons": return "Hierarchy カスタムをリセット";
            case "ResetAllSettingsMessage": return "すべての設定、色、オブジェクト上書きをリセットし、ツールウィンドウを閉じますか？";
            case "ResetAll": return "すべてリセット";
            case "SelectedObjectLineColor": return "選択オブジェクト接続線色";
            case "SelectedObjectLineColorHelp": return "選択した GameObject の接続線色を適用または解除します。";
            case "ShowParentLines": return "親接続線を表示";
            case "IncludeChildren": return "子を含める";
            case "ApplyToListed": return "一覧に適用";
            case "ClearListed": return "一覧を解除";
            case "Scope": return "範囲";
            case "ScopeWithChildren": return "{1} 個の選択ルートから {0} 個の編集対象";
            case "ScopeSelectedOnly": return "{0} 個の選択オブジェクト";
            case "CustomIconAssets": return "カスタムアイコンアセット";
            case "CustomIconAssetsHelp": return "プロジェクト内のカスタム Hierarchy アイコンをインポート、選択、削除します。";
            case "ImportLocalIcon": return "ローカルアイコンをインポート";
            case "DeleteSelected": return "選択を削除";
            case "ImportedIconCount": return "インポート済みアイコン数";
            case "ImportedIconCountValue": return "{0}（選択 {1}）";
            case "NoCustomIcons": return "カスタムアイコンはまだインポートされていません。";
            case "DeleteImportedIcons": return "インポート済みアイコンを削除";
            case "DeleteImportedIconsMessage": return "選択したインポート済みアイコンアセット {0} 個をプロジェクトから削除しますか？";
            case "Delete": return "削除";
            case "Cancel": return "キャンセル";
            case "DeletedImportedIconsMessage": return "インポート済みアイコンアセット {0} 個を削除しました。";
            case "OK": return "OK";
            case "ThisObjectOnly": return "このオブジェクトのみ";
            case "RelatedObjectsDetected": return "関連オブジェクト {0} 個を検出";
            case "ObjectName": return "オブジェクト名";
            case "RenameObject": return "オブジェクト名を変更";
            case "ApplyIcon": return "アイコンを適用";
            case "NameColor": return "名前色";
            case "MainColor": return "メイン色";
            case "LineColor": return "接続線色";
            case "ApplyNamedValue": return "{0}を適用";
            case "Current": return "現在";
            case "UnityIcons": return "Unity アイコン";
            case "ImportedIcons": return "インポート済みアイコン";
            case "ComponentIcons": return "コンポーネントアイコン";
            case "SyncLine": return "線を同期";
            case "SyncColorsAndIcon": return "色とアイコンを同期";
            case "FullPalette": return "全体パレット";
            case "Close": return "閉じる";
            case "ObjectsWithCustomStyle": return "カスタムスタイル付きオブジェクト";
            case "ObjectsWithCustomStyleHelp": return "ロード済みオブジェクトの Hierarchy 色またはアイコン設定を確認します。";
            case "SelectedOnly": return "選択のみ";
            case "Search": return "検索";
            case "ClearSearch": return "検索をクリア";
            case "Refresh": return "更新";
            case "CleanMissing": return "欠落を整理";
            case "ObjectsWithCustomParameters": return "カスタムパラメーター付きオブジェクト";
            case "NoMatchingObjects": return "現在のスタイルフィルターに一致するロード済み GameObject はありません。";
            case "ClearListedObjects": return "一覧オブジェクトを解除";
            case "ClearListedObjectsMessage": return "一覧の GameObject から Hierarchy 色とカスタムアイコンを解除しますか？";
            case "Clear": return "解除";
            case "NoMissingRecords": return "ロード済みシーンに欠落したスタイル記録はありません。";
            case "RemovedMissingRecords": return "欠落したスタイル記録 {0} 件を削除しました。";
            case "Object": return "オブジェクト";
            case "AppliedParameters": return "適用済みパラメーター";
            case "Actions": return "操作";
            case "Status": return "状態";
            case "Main": return "メイン";
            case "Line": return "線";
            case "Name": return "名前";
            case "Icon": return "アイコン";
            case "Select": return "選択";
            case "Edit": return "編集";
            case "Default": return "既定";
            case "Presets": return "プリセット";
            default: return EnglishText(key);
        }
    }
}
#endif
