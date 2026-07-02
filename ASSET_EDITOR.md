# Asset editor

Press **Tab** in-game to open the asset editor. Press **Tab** again or click **Close** to exit. Changes save automatically.

The editor panel has two columns:

- **Left** — list of assets or produce definitions, with New / Clone / Delete
- **Right** — editing controls (canvas, tools, colors, etc.)

Switch between **Assets** and **Produce** using the tabs at the top of the left column.

## Assets tab

Create and edit pixel-art sprites used by crops, decorations, and the in-game asset library.

### Asset list (left column)

| Button | Action |
|--------|--------|
| **New** | Start a blank 16×16 asset |
| **Clone** | Duplicate the selected asset (named `Original_2`, `Original_3`, …) |
| **Delete** | Remove the asset and its world placements |

Click an asset in the list to select it. The list shows each asset's **display id** (which may differ from the filename). Scroll the list with the mouse wheel.

### Canvas (right column)

- **Name** — display name for the asset. Renaming changes the filename on save.
- **Size** — `-W` / `+W` / `-H` / `+H` adjust canvas size (1–32 pixels per side).
- **Paint** — left-click to draw, right-click to erase (regardless of tool).
- **Clear** — wipe all pixels on the canvas.

### Tools

| Tool | Usage |
|------|--------|
| **Brush** | Paint with the active color. Shortcut: **B** |
| **Select** | Drag to select a rectangle; drag the selection to move it. Shortcut: **S** |
| **Pick** | Click a pixel to sample its color, then return to Brush automatically. Shortcut: **I** |
| **Undo** | Revert the last change (up to 50 steps). Shortcut: **Ctrl+Z** / **Cmd+Z** |
| **Redo** | Reapply the last undone change. Shortcut: **Ctrl+Y** / **Cmd+Shift+Z** |

### Colors

- **Palette** — eight preset swatches; click one to select it.
- **X** — eraser (transparent pixels).
- **Custom swatch** — shows the current custom color; click to use it.
- **Darken** / **Lighten** — adjust the current color (~15% darker or lighter).
- **Recent** — up to six recently used colors; click to select.
- **Color picker** — full HSV/RGB control below the palette:
  - Drag the saturation/value box and hue strip
  - Drag the R, G, B sliders
  - HSV and hex values are shown at the bottom

### Place

Click **Place**, then click on the game map to drop the asset as a decoration. Placements are saved to `placements.json`.

## Produce tab

Define harvestable produce (animated frame sequences for crops like tomato and corn). Produce files live in `FarmGame/Assets/` alongside crop JSON.

### Produce list (left column)

| Button | Action |
|--------|--------|
| **New** | Create a new produce definition |
| **Clone** | Duplicate the selected produce (`Tomato_2`, etc.) |
| **Delete** | Remove the produce file |

Click a produce in the list to select it. The list shows each produce's **display name** (which may differ from the filename). Scroll with the mouse wheel.

### Produce editor (right column)

- **Name** — produce display name (e.g. `Tomato`).
- **Frames** — ordered list of asset IDs that make up the animation. Use **↑** / **↓** to reorder and **×** to remove a frame. Missing or unresolved asset references are shown in **red**; saving still works, but the status bar warns which frames could not be found.
- **Add frame** — click an asset in the picker below to append it to the frames list.

Example produce file (`FarmGame/Assets/tomato.json`):

```json
{
  "name": "Tomato",
  "frames": [
    "Seeds",
    "Sprout",
    "Sprout_2",
    "Buds",
    "Tomato_1",
    "Tomato_2"
  ]
}
```

Crop growth animations in-game resolve frames from produce definitions by crop name (e.g. `CropType.Tomato` → `Tomato.json`).

## File storage

| What | Location | Format |
|------|----------|--------|
| Pixel sprites | `FarmGame/Assets/defined/*.png` | PNG images |
| Asset metadata | `FarmGame/Assets/defined/*.meta.json` | Optional; only when display `id` differs from filename |
| World placements | `FarmGame/Assets/defined/placements.json` | JSON list of asset name + x/y |
| Produce definitions | `FarmGame/Assets/*.json` (files with a `frames` array) | JSON |

Display names can differ from filenames. Clone naming uses underscores: `Tomato` → `Tomato_2`, `Tomato_3`, …
