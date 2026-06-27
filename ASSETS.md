# Third-party assets

This file lists **files in the repo** (or copied to build output) that come from outside the project, with their license and source. Add a row here whenever you import a new asset.

| Asset | Location in repo | Source | License | Notes |
|--------|------------------|--------|---------|--------|
| Open Sans (variable font) | `FarmGame/Fonts/OpenSans.ttf` | [Google Fonts — Open Sans](https://github.com/google/fonts/tree/main/ofl/opensans) (`OpenSans[wdth,wght].ttf`) | [SIL Open Font License 1.1](https://openfontlicense.org/) | Full license text: `FarmGame/Fonts/OpenSans-OFL.txt`. Used by `UiText` for UI text. |
| **Cute Fantasy RPG — Free** (`Cute_Fantasy_Free.zip`) | `FarmGame/Assets/cute-fantasy-free/Cute_Fantasy_Free/` · attribution: [`ATTRIBUTION.txt`](FarmGame/Assets/cute-fantasy-free/ATTRIBUTION.txt) · pack readme: `read_me.txt` | [Cute Fantasy RPG — Kenmi on itch.io](https://kenmi-art.itch.io/cute-fantasy-rpg) | **Free version (in-repo):** non-commercial use only; modify freely; **do not redistribute or resell**, even if modified. **Credit required:** Kenmi or the itch.io link (see attribution file). | **Contents:** `Tiles/` (grass, water, path, cliff, beach, farmland), `Player/` (`Player.png`, `Player_Actions.png`), `Enemies/` (skeleton, green slime), `Animals/` (chicken, cow, pig, sheep), `Outdoor decoration/` (trees, house, bridge, fences, chest, props sheet). Add a `<None Include=... CopyToOutputDirectory>` entry in `FarmGame.csproj` when wiring assets at runtime. |

## How to add an entry

1. Place the file under a clear path (e.g. `FarmGame/Fonts/`, `FarmGame/Assets/`).
2. If the license requires it, add the project’s license file beside the asset (or a `*-OFL.txt` / `LICENSE` copy).
3. Reference it in the project file (`FarmGame.csproj`) if it must be copied to build output.
4. Append a row to the table above with **source URL or project**, **license name + link**, and **short notes**.

## NuGet packages

Runtime dependencies (e.g. Raylib-cs) are governed by their respective package licenses on NuGet; they are not listed here as vendored assets.
