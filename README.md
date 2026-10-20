# ESCAPE ROOM

[![Unity](https://img.shields.io/badge/Unity-6000.4.1f1-000000?logo=unity&logoColor=white)](https://unity.com)
[![URP](https://img.shields.io/badge/Render%20Pipeline-URP-4B8BBE)](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.4/manual/index.html)
[![Input System](https://img.shields.io/badge/Input-New%20Input%20System-2C5F2D)](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/index.html)
[![Platform](https://img.shields.io/badge/Platform-Windows-0078D6?logo=windows&logoColor=white)](#requirements)

A first-person escape-room game built in Unity 6. Locked in a dim study, you must read the
diary to learn the riddle, find four ritual items hidden around the room, place them on the
pedestals, and open the gate — all before the ten-minute timer runs out.

> **Repository:** [github.com/Palak-eng/ESCAPE-ROOM](https://github.com/Palak-eng/ESCAPE-ROOM)

## Table of contents

- [About](#about)
- [Features](#features)
- [Screenshots](#screenshots)
- [Requirements](#requirements)
- [Getting started](#getting-started)
- [Controls](#controls)
- [How to play](#how-to-play)
- [Project structure](#project-structure)
- [Architecture](#architecture)
- [Technical notes](#technical-notes)
- [Assets and credits](#assets-and-credits)
- [Contributing](#contributing)
- [License](#license)

## About

**ESCAPE ROOM** is a small, self-contained first-person puzzle game. Everything is
driven by C# scripts that build the UI and game systems at runtime: the interaction
prompts, diary reader, journal, ritual-item inventory, pedestal logic, gate, and the
countdown timer. The scene itself holds only the environment and a handful of component
attachments, which keeps the level easy to edit and the systems reusable.

## Features

- **Explore in first person** with WASD + mouse look, built on Unity's Starter Assets controller.
- **Interactive diary** — open it on the sofa, read the page fullscreen, and collect it.
- **Journal & re-reading** — press `T` to bring the latest diary page back up at any time.
- **Carryable torch** with its own warm, shadowless light for searching dark spaces.
- **Four ritual items** — alarm clock, book, photograph, and key — scattered through the room.
- **Pedestal puzzle** — place each item on a pedestal; all four must be filled to open the gate.
- **Timed escape** — a 10-minute countdown that pauses while you read.
- **Win / lose flow** — end-of-run panel with stats, restart, and keep-exploring options.
- **Runtime-generated UI** with no prefab dependencies.

## Screenshots

Add gameplay captures to `docs/screenshots/` and reference them here:

```markdown
![Main room](docs/screenshots/main-room.png)
![Diary reader](docs/screenshots/diary.png)
![Pedestals](docs/screenshots/pedestals.png)
```

## Requirements

| Requirement | Version |
| --- | --- |
| Unity Editor | `6000.4.1f1` (Unity 6) |
| Render pipeline | URP 17.4 |
| Input | Input System 1.19 (new Input System enabled) |
| OS tested | Windows |

## Getting started

```bash
git clone https://github.com/Palak-eng/ESCAPE-ROOM.git
cd ESCAPE-ROOM
```

1. Open the folder in **Unity Hub** with Unity `6000.4.1f1` (or a compatible Unity 6 release).
2. Let Unity import the assets and resolve packages from `Packages/manifest.json`.
3. Open `Assets/Scenes/scene.unity`.
4. Press **Play**.

`scene.unity` is the only scene in Build Settings. The runtime managers create themselves,
so no extra scene setup is needed.

## Controls

| Key | Action |
| --- | --- |
| `W` `A` `S` `D` | Move |
| Mouse | Look |
| `E` | Interact — open the diary, pick up the torch, collect items, place items on pedestals |
| `O` | Open / close drawers and boxes |
| `R` | Read the diary page in fullscreen (while reading) |
| `Esc` | Close the diary / collect it into the journal |
| `T` | Open the latest diary page fullscreen |
| `J` / `Tab` / `C` | Open the journal list |

The `T : Diary` entry in the on-screen legend appears after the first diary is collected.

## How to play

1. **Find the diary** on the sofa. Press `E` to open it, `R` to read the full page, and `Esc`
   to collect it. The riddle is shown in the fullscreen reader.
2. **Pick up the torch** with `E`. It carries its own light for dark drawers and corners.
3. **Search the room.** Drawers and boxes open with `O`; loose items are picked up with `E`.
   The four ritual items are:
   - Alarm clock
   - Book
   - Photograph
   - Key (hidden in a drawer)
4. **Fill the pedestals.** Carry each item to a pedestal and press `E` to place it.
5. **Open the gate.** When all four pedestals are filled, `wall_with_small_door` opens.
6. **Escape** before the 600-second timer reaches zero.

## Project structure

```text
Assets/
  Scenes/
    scene.unity          Main level (game scene)
    level1.unity         Early / backup level
  3d_models/             Room geometry and props
  images/                Diary page art and textures
  music/                 Ambient horror loop, pedestal rumble
  Resources/
    aim.png              Runtime crosshair / aim reticle
    treasure_box.png     Pickup icon
  scripts/               Gameplay code (see Architecture)
  Starter Assets/        First-person controller package
  Settings/              URP assets
Packages/                Unity package manifest
ProjectSettings/         Unity project configuration
```

## Architecture

| Script | Responsibility |
| --- | --- |
| `PlayerInteractor.cs` | Camera raycast, nearest-target resolution, exposes `CurrentTarget`. |
| `IInteractable.cs` | `IInteractable` and `IDeferTargeting` contracts. |
| `InteractionUI.cs` | Interaction prompts, controls legend, aim reticle. |
| `DiaryInteractable.cs` | Sofa diary: closed/open states, `E`→`R`→`Esc` flow, collider auto-fit. |
| `DiaryUIManager.cs` | Fullscreen diary reader: HD page, title, hints, torchlight glow, story text. |
| `CollectiblesManager.cs` | Collected diaries, journal (`J`/`Tab`/`C`), `T` fullscreen read, toasts. |
| `DiaryBootstrap.cs` | Auto-creates runtime managers if missing. |
| `QuestInventory.cs` | Ritual-item inventory, single hand-held item, HUD. |
| `CollectibleItem.cs` | `E` pickup, hands-full feedback, collider auto-fit. |
| `PedestalSlot.cs` | Accepts the held item, centers and sinks it with rumble audio. |
| `PedestalGate.cs` | Opens the gate once every pedestal slot is filled. |
| `PedestalGateHint.cs` | Hint text near the gate. |
| `DrawerInteractable.cs` | `O` opens/closes drawers; defers targeting while open. |
| `BoxInteractable.cs` | `O` opens/closes the box and lid. |
| `TorchPickup.cs` | `E` pickup, camera-attached torch light, `HasTorch` state. |
| `GameTimer.cs` | 10-minute countdown, pause while reading, win/lose panel, restart. |
| `BGMManager.cs` | Ambient background music loop. |

### Interaction flow

```
PlayerInteractor ──raycast──▶ IInteractable.CurrentTarget
      │                              │
InteractionUI ◀── prompt text ───────┘
      │
      └── E / O ──▶ OnInteract() ──▶ Diary / Item / Pedestal / Drawer / Torch
```

## Technical notes

- **New Input System only.** Scripts use `Keyboard.current` and the Input System API. Do not
  add a `StandaloneInputModule`; `GameTimer` creates an `InputSystemUIInputModule` event system.
- **Runtime UI uses legacy `UnityEngine.UI.Text`.** TextMeshPro components created at runtime
  have no font asset and render invisibly, so generated labels use the built-in
  `LegacyRuntime.ttf` font.
- **Colliders self-correct.** `CollectibleItem`, `DiaryInteractable`, and pedestal placement
  derive sizes from mesh bounds. Avoid dragging collider handles in the Scene view; type exact
  values in the Inspector instead.
- **Aim reticle** loads from `Resources/aim.png` and preserves the image's aspect ratio.
- **No `.meta` churn** — keep Unity-generated `.meta` files in commits so references survive.

## Assets and credits

Third-party assets are used under their respective licenses. If you reuse this project,
you must keep these credits and comply with each source license (several require
attribution — **CC BY**).

### 3D models (Sketchfab)

| Asset | Author | License | Source |
| --- | --- | --- | --- |
| Diary / PSX Book | 𝙯𝙞𝙧𝙤 ([@ziro3d](https://sketchfab.com/ziro3d)) | Sketchfab Free Standard | [skfb.ly/pMCUP](https://skfb.ly/pMCUP) |
| Vintage alarm clock / Bell Alarm Clock | stanislau.see | CC BY 4.0 | [skfb.ly/oGRQW](https://skfb.ly/oGRQW) |
| Silver key / Key | Mr NISHKE ([@Sjgtrudeau15](https://sketchfab.com/Sjgtrudeau15)) | CC BY 4.0 | [skfb.ly/6zWTC](https://skfb.ly/6zWTC) |
| Vintage book / Vintage Book A301 | Chellew ([@chellewwxy](https://sketchfab.com/chellewwxy)) | CC BY 4.0 | [skfb.ly/6TV8G](https://skfb.ly/6TV8G) |
| Game-Ready Cardboard Boxes | jellystuff | Sketchfab Free Standard | [Sketchfab](https://sketchfab.com/3d-models/game-ready-cardboard-boxes-57e08a9a9c7e4be99e906a093405e590) |
| Pedestal / STL Socle Simple (free print ready) | See model page | Sketchfab Free Standard | [Sketchfab](https://sketchfab.com/3d-models/stl-socle-simple-free-print-ready-2v-cb01151df4ac4c12ac61fff11d78cd7f) |
| Torch / Low Poly Torch | ChillociRaptor | CC BY 4.0 | [skfb.ly/opIyQ](https://skfb.ly/opIyQ) |

### Audio

| Asset | Author | License | Source |
| --- | --- | --- | --- |
| Ambient horror loop | alex-morgan | Pixabay Content License | [Pixabay](https://pixabay.com/music/ambient-ambient-horror-creepy-atmosphere-dark-587402/) |
| Pedestal rumble ("earth magic (3)") | Yodguard | Pixabay Content License | [Pixabay](https://pixabay.com/sound-effects/film-special-effects-earth-magic-3-378600/) |

### Other

- First-person controller and starter assets: Unity **Starter Assets** package.
- Environment models and textures: see `Assets/3d_models/` and `Assets/images/`.

> **CC BY notice:** For assets marked CC BY, credit must name the author, link to the
> license, and indicate if changes were made. Models were imported, scaled, and adapted for
> this scene.

## Contributing

1. Fork the repository and create a feature branch.
2. Keep the project on the new Input System and runtime-built `UnityEngine.UI.Text` UI.
3. Do not commit `Library/`, `Temp/`, `Logs/`, or IDE files (see `.gitignore`).
4. Open a pull request describing the change and any scene edits.

## License

Released under an **MIT-based license with an attribution requirement** — see [`LICENSE`](LICENSE).

You may use, copy, modify, merge, publish, distribute, sublicense, and sell copies of this
project, **provided that**:

- The copyright notice and license text are included in all copies or substantial portions.
- Clear credit is given to the original author, **Palak**
  ([github.com/Palak-eng](https://github.com/Palak-eng)), with a link back to
  [github.com/Palak-eng/ESCAPE-ROOM](https://github.com/Palak-eng/ESCAPE-ROOM).

Using this project without giving credit is not permitted.

Copyright © 2026 Palak. All rights reserved as described in the license above.
