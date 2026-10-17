<div align="center">

<h1 id="custom-hierarchy">🎨 Custom Hierarchy</h1>

<p><strong>Give every GameObject a little personality. ✨</strong></p>
<p>Component icons, colorful scene organization, and quick styling tools for the Unity Editor.</p>

<p>
  <img alt="Unity Editor extension" src="https://img.shields.io/badge/Unity-Editor%20Extension-222222?style=for-the-badge&amp;logo=unity&amp;logoColor=white">
  <img alt="Written in C Sharp" src="https://img.shields.io/badge/Language-C%23-8B5CF6?style=for-the-badge">
  <a href="LICENSE"><img alt="License: GPL v3" src="https://img.shields.io/badge/License-GPL%20v3-22C55E?style=for-the-badge"></a>
</p>

<p>
  <a href="#features">Features</a> ·
  <a href="#installation">Installation</a> ·
  <a href="#quick-start">Quick Start</a> ·
  <a href="#palette">Palette Guide</a> ·
  <a href="#previews">Previews</a> ·
  <a href="#faq">FAQ</a>
</p>

</div>

---

Custom Hierarchy helps you find your way around busy Unity scenes. See attached components beside each GameObject, distinguish groups with row and name colors, and follow parent-child relationships with colored connection lines. A quick popup handles individual edits, while the **Hierarchy Palette** brings display settings, imported icons, and styled objects into one place.

> [!TIP]
> **Your first style in a few clicks:** focus the Hierarchy, hover over a GameObject, press <kbd>Alt</kbd>, choose your colors and icon, then click **Apply All**.

<a id="features"></a>

## ✨ Features

| Tool | What you can do |
| --- | --- |
| 🧩 **Component icons** | See attached components directly in the Hierarchy and choose whether to display the Transform icon. |
| 🎨 **Main row colors** | Add a color stripe and subtle row tint to make important objects and groups easier to spot. |
| 🔤 **Name colors** | Give individual GameObject names their own text color. |
| 🌿 **Connection lines** | Follow parent-child relationships with a global line color or per-object overrides. |
| 🖼️ **Custom object icons** | Choose Unity built-in icons, icons from attached components, or images imported from your computer. |
| ⚡ **Quick popup** | Rename an object, search icons, choose preset colors, and apply a complete style from one window. |
| 📦 **Batch styling** | Apply main and line colors to a selection or a selection with its children; synchronize popup styles across related objects. |
| 🗂️ **Changed Object Manager** | Find loaded objects with custom styles, search by name, hierarchy path, or icon name, and edit or clear their overrides. |
| 🌐 **Language options** | Choose Auto, English, Simplified Chinese, Korean, or Japanese for the localized window controls. |
| 💾 **Project settings** | Keep display preferences and object styles in a project-local JSON file. |
| 🧹 **Cleanup tools** | Remove missing style records from loaded scenes, clear individual overrides, or reset the tool's settings. |

<a id="installation"></a>

## 📥 Installation

### Option 1 — Copy the source files

1. Download or clone this repository.
2. Copy the entire [`Editor/Custom Hierarchy`](Editor/Custom%20Hierarchy) folder into your Unity project's `Assets/Editor/` folder.
3. Wait for Unity to import and compile the scripts.
4. Open **Tools → Hierarchy Component Icons** to access the display options and palette.

Your project should contain this folder structure:

```text
Assets/
└── Editor/
    └── Custom Hierarchy/
        ├── HierarchyComponentIcons.cs
        ├── HierarchyComponentIcons.Drawing.cs
        ├── HierarchyComponentIcons.IconCatalog.cs
        ├── HierarchyComponentIcons.Localization.cs
        ├── HierarchyComponentIcons.Menu.cs
        ├── HierarchyComponentIcons.ObjectStyle.cs
        ├── HierarchyComponentIcons.Preferences.cs
        ├── HierarchyComponentIcons.ProjectSettings.cs
        ├── HierarchyComponentIconPaletteWindow.cs
        └── HierarchyObjectColorPopupWindow.cs
```

### Option 2 — Import a Unity package

If you have a `.unitypackage` distribution of Custom Hierarchy:

1. Open your Unity project.
2. Choose **Assets → Import Package → Custom Package…**.
3. Select the `.unitypackage`, review its contents, and click **Import**.
4. Wait for compilation, then open **Tools → Hierarchy Component Icons**.

The extension uses Unity Editor scripts. Keep the source files together under an `Editor` folder. This repository does not specify a minimum supported Unity version.

<a id="quick-start"></a>

## 🚀 Quick Start

1. **Open a scene** and focus the Hierarchy window.
2. **Hover over the GameObject's row** and press <kbd>Alt</kbd>. You can also hold <kbd>Alt</kbd> and left-click or right-click the row.
3. **Choose an icon** from the component, imported, or Unity icon grids. Use **Search** to narrow the choices.
4. **Set your colors:**
   - **Name Color** changes the object's name text.
   - **Main Color** changes the row highlight.
   - **Line Color** changes its hierarchy connection lines.
5. **Review the scope.** Check **Sync Line** and **Sync Colors And Icon** if related objects should receive the changes.
6. Click **Apply All** to save the popup edits and close it.

The popup also lets you edit **Object Name** before applying. Choose **Full Palette** to open the management window, or **Close** to dismiss the popup without applying its pending style and rename edits.

> [!NOTE]
> Related objects include the target and its descendants. When the target belongs to a multi-selection, they include the selected objects and their descendants. **Sync Line** is enabled by default when related objects exist; **Sync Colors And Icon** is enabled by default for a target in a multi-selection. Review both toggles before applying.

### 🖱️ Component icon shortcuts

| Action | Result |
| --- | --- |
| Click a component icon | Toggle its enabled state when the component is a `Behaviour`, `Renderer`, or `Collider`. |
| <kbd>Ctrl</kbd> + click, or <kbd>⌘</kbd> + click | Select and ping the component in the Editor. |
| **Tools → Hierarchy Component Icons → Display → Show Transform Icon** | Show or hide Transform icons. |

Component toggles change the component's actual enabled state and support Unity Undo.

<a id="palette"></a>

## 🛠️ Hierarchy Palette Guide

Open **Tools → Hierarchy Component Icons → Palette → Open Color Palette**. The same command is available under **GameObject → Hierarchy Component Icons → Palette** and through the popup's **Full Palette** button.

The palette has collapsible sections and scrollbars so you can reach its controls even when the window is smaller than its contents.

<details open>
<summary><strong>🎛️ Display Settings — language and connection lines</strong></summary>

- Choose a window language. **Auto** checks project language settings first, then the Editor and system language.
- Turn **Show Parent Lines** on or off.
- Enable **Use Custom Color** to choose a global connection line color, or use the preset swatches.
- Choose **Use Skin Default** to return to the default line appearance.

The Unity menu paths remain in English when the window language changes.

</details>

<details>
<summary><strong>🎨 Selected Object Tools — default popup colors</strong></summary>

Enable **Use Custom Default Colors** to choose the Main, Name, and Line colors offered by the Alt popup for objects without saved colors. Each color has **Default** and **Random** controls, and the preset palette can set all three together.

When custom defaults are off, the popup generates random colors for color channels without saved values. Existing saved colors are loaded when you reopen an object's popup.

</details>

<details>
<summary><strong>🖼️ Imported Icon Library — bring your own images</strong></summary>

1. Click **Import Local Icon** in the palette or the quick popup.
2. Choose a `.png`, `.jpg`, `.jpeg`, `.tga`, `.psd`, or `.bmp` file.
3. Open an object's popup and choose the image from **Imported Icons**.
4. Click **Apply All** to assign it.

Imported images are copied into `Assets/Editor/Custom Hierarchy/Custom Icons/`. In the palette, select imported icons and use **Delete Selected** to remove their copied assets after confirming the dialog.

</details>

<details>
<summary><strong>🗂️ Changed Object Manager — find, edit, and clean up styles</strong></summary>

- Review loaded GameObjects with custom Main, Line, Name, or Icon values.
- Enable **Selected Only** to narrow the list to your current selection.
- Search by object name, hierarchy path, or icon name. Matching is case-insensitive.
- Use **Select** to locate an object, **Edit** to open its popup, or **Clear** to remove all of its style overrides.
- Adjust color values directly in the list, or use **X** to remove an individual color or icon override.
- Use **Clear Listed** to remove the styles from the currently listed objects after confirmation.
- Use **Clean Missing** to remove stale style records for deleted objects in loaded scenes. Records belonging to unopened scenes are retained.

</details>

### 📦 Apply colors to a group

Select your GameObjects, then open **GameObject → Hierarchy Component Icons → Main Color** or **Line Color**.

| Menu command | Scope |
| --- | --- |
| **Apply To Selection** | Apply the stored Main or Line color to the selected objects. |
| **Apply To Selection And Children** | Apply that color to the selected objects and all descendants. |
| **Clear Selection** | Remove that color override from the selected objects. |
| **Clear Selection And Children** | Remove that color override from the selected objects and all descendants. |

The commands use the Main and Line color values configured in the palette. For icons and name colors across related objects, use **Sync Colors And Icon** in the quick popup.

> [!TIP]
> Try a simple scene color key: 🔵 gameplay, 🟢 environment, 🟠 lighting, and 🟣 UI. Give each group root a matching icon, then apply colors to its children for a consistent visual guide.

<a id="previews"></a>

## 📸 Previews

### 🌈 A more readable Hierarchy

Component icons, custom object icons, colored names, row highlights, and connection lines help distinguish objects at a glance.

<p align="center">
  <img src="img/Screenshot%202026-08-08%20012841.png" alt="Unity Hierarchy with component icons, custom object icons, colored rows and names, and parent-child connection lines" width="358">
</p>

<details>
<summary><strong>🛠️ Expand the Hierarchy Palette preview</strong></summary>

The palette brings display controls, icon imports, and customized object management together. This screenshot shows an earlier layout; some controls may differ from the current source.

<p align="center">
  <img src="img/Screenshot%202026-08-08%20012830.png" alt="Hierarchy Palette showing display settings, selected object tools, imported icon library, and changed object manager" width="854">
</p>

</details>

<a id="faq"></a>

## 💡 FAQ & Troubleshooting

<details>
<summary><strong>Why doesn't the Alt popup open?</strong></summary>

Focus the Hierarchy window and keep the pointer over a GameObject's row when pressing <kbd>Alt</kbd>. Alternatively, hold <kbd>Alt</kbd> and click the row. Check that **Tools → Hierarchy Component Icons → Display → Enabled** is on.

</details>

<details>
<summary><strong>Why are row colors or connection lines hidden?</strong></summary>

Under **Tools → Hierarchy Component Icons → Display**, enable **Show Main Colors** for row colors and **Show Parent Lines** for connections. Also check the object's saved color and alpha value. Transform icons have their own **Show Transform Icon** toggle.

</details>

<details>
<summary><strong>Where are my settings saved?</strong></summary>

Preferences and object styles are stored in `Assets/Editor/Custom Hierarchy/HierarchyComponentIconsSettings.json`. Changes are held in memory and written when a scene closes, scripts reload, or Unity quits. Save your scenes so objects have stable identities across Editor sessions.

To share the styles with your project, include the generated settings file and any imported icon assets in version control alongside your scene files and their `.meta` files.

</details>

<details>
<summary><strong>How do I clear styles or reset everything?</strong></summary>

For a single object, use **Clear** in the Changed Object Manager. For an individual popup color or icon, click **Clear**, then **Apply All**. Use the GameObject menu's clear commands for a selection or its children.

**Tools → Hierarchy Component Icons → Emergency → Reset All** opens a confirmation dialog, then resets preferences and stored object overrides and closes the tool windows. Use **Emergency → Clear Blocking UI** if you only need to close a stuck popup.

</details>

<details>
<summary><strong>Why can't I see the tool's menu after installation?</strong></summary>

Confirm that all ten C# files are together under an `Editor` folder in your project's `Assets` directory. Wait for Unity's compilation to finish and check the Console for compile errors.

</details>

## 🤝 Contributing

Found a bug or have an idea? [Open an issue](https://github.com/ShadowsLunarfox/Custom-Hierarchy-for-Unity/issues) with your Unity version, steps to reproduce, and a screenshot or Console message when relevant. Pull requests for fixes, documentation, and translations are welcome.

## 📄 License

Distributed under the [GNU General Public License v3.0](LICENSE).

---

<p align="center">✨ A little color goes a long way. Happy scene building! ✨<br><a href="#custom-hierarchy">Back to top ↑</a></p>
