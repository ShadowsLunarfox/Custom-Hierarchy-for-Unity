# Custom Hierarchy

Custom Hierarchy is a Unity Editor extension for improving the Hierarchy window. It adds component icons, object colors, connection line colors, name colors, custom icons, and a management palette so scene objects are easier to identify and organize.

## Features

- Display component icons next to GameObjects in the Hierarchy.
- Toggle the Transform icon display.
- Show parent-child hierarchy connection lines.
- Set a main row color for individual GameObjects.
- Set a custom connection line color for individual GameObjects.
- Set a custom name color for individual GameObjects.
- Assign Unity built-in icons or component icons to GameObjects.
- Edit object hierarchy style from a quick popup window.
- Apply colors to selected objects or selected objects with children.
- Manage changed objects from a centralized palette window.
- Search changed objects by name, path, or icon name.
- Copy one object's style and paste it to the selected objects.
- Clean missing style records from loaded scenes.
- Scroll the palette window when the UI does not fit on screen.
- Reset or clear custom settings when needed.

## Installation

1. Download the `.unitypackage` file from the release page or the download location provided by the author.

2. Open your Unity project.

3. In Unity, open the package import menu:

   ```text
   Assets > Import Package > Custom Package...
   ```

4. Select the downloaded `.unitypackage` file.

5. In the import window, keep the plugin files selected.

6. Click:

   ```text
   Import
   ```

7. Wait for Unity to finish importing and compiling scripts.

8. After compilation, the tool menu should appear at:

   ```text
   Tools > Hierarchy Component Icons
   ```

## Basic Usage

Enable or disable the extension:

```text
Tools > Hierarchy Component Icons > Display > Enabled
```

Open the palette window:

```text
Tools > Hierarchy Component Icons > Palette > Open Color Palette
```

Use GameObject context menu options:

```text
GameObject > Hierarchy Component Icons
```

## Display Options

The display options are available under:

```text
Tools > Hierarchy Component Icons > Display
```

Available options:

- `Enabled`: turns the Hierarchy extension on or off.
- `Show Transform Icon`: shows or hides the Transform component icon.
- `Show Parent Lines`: shows or hides hierarchy connection lines.
- `Show Main Colors`: shows or hides object row colors.

## Object Styling

Objects can be customized with:

- Main row color
- Connection line color
- Name color
- Custom icon

You can apply or clear styles from the GameObject menu, the object popup window, or the palette window.

## Palette Window

Open the palette window from:

```text
Tools > Hierarchy Component Icons > Palette > Open Color Palette
```

The palette window includes:

- Parent line color controls
- Selected object line color controls
- Editable selected object list
- Changed objects list
- Search field for changed objects
- Copy and paste style controls
- Missing record cleanup
- Bulk clear controls

## Copy And Paste Style

1. Open the palette window.
2. Find an object in the `Changed Objects` list.
3. Click `Copy` on that object.
4. Select one or more GameObjects in the Hierarchy.
5. Click `Paste To Selection`.

Options:

- `Paste Children`: also paste the copied style to selected objects' children.
- `Paste Exact Style`: remove target parameters that are not present on the copied style.

## Cleanup

Use:

```text
Clean Missing
```

This removes style records for missing objects in currently loaded scenes. Records from unloaded scenes are kept to avoid deleting valid configuration from other scenes.

## Reset

Emergency reset options are available at:

```text
Tools > Hierarchy Component Icons > Emergency
```

Available options:

- `Clear Blocking UI`
- `Reset All`

## Uninstallation

To uninstall the extension, delete the imported plugin files from your Unity project and let Unity recompile.

If the tool was imported from a `.unitypackage`, remove the imported `Custom Hierarchy` or `Hierarchy Component Icons` files from the `Assets` folder.

## Notes

- This tool is an Editor-only extension.
- It does not affect builds or runtime gameplay.
- Object style data is stored in Unity Editor preferences.
- Some Unity built-in icon names can vary between Unity versions; unavailable icons are skipped or replaced with compatible fallbacks.
