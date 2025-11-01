# Unity Essentials

This module is part of the Unity Essentials ecosystem and follows the same lightweight, editor-first approach.
Unity Essentials is a lightweight, modular set of editor utilities and helpers that streamline Unity development. It focuses on clean, dependency-free tools that work well together.

All utilities are under the `UnityEssentials` namespace.

```csharp
using UnityEssentials;
```

## Installation

Install the Unity Essentials entry package via Unity's Package Manager, then install modules from the Tools menu.

- Add the entry package (via Git URL)
    - Window → Package Manager
    - "+" → "Add package from git URL…"
    - Paste: `https://github.com/CanTalat-Yakan/UnityEssentials.git`

- Install or update Unity Essentials packages
    - Tools → Install & Update UnityEssentials
    - Install all or select individual modules; run again anytime to update

---

# Camera Render Texture Handler

> Quick overview: Camera output is directed to a managed RenderTexture with configurable size, HDR, mipmaps, filter mode, and aspect‑ratio aware UI presentation.

A camera’s render target is managed at runtime and exposed as a static `RenderTexture` for convenient access. Dimensions, HDR/SDR, mipmapping, and filter mode can be configured; aspect‑ratio handling is applied to a small UI Toolkit overlay so the texture is displayed letterboxed/pillarboxed correctly regardless of screen size. When a Scriptable Render Pipeline supports render requests, a one‑shot request can be submitted to populate the target.

![screenshot](Documentation/Screenshot.png)

## Features
- RenderTexture management
  - Configurable width/height with minimum size validation
  - Optional HDR (R16G16B16A16_SFloat) vs. sRGB (R8G8B8A8_SRGB)
  - Mipmaps on/off with auto‑generation
  - Filter mode selection (Point/Bilinear/Trilinear)
- Aspect‑ratio handling
  - Optional explicit aspect ratio via numerator/denominator; falls back to screen aspect when unset
  - Runtime letterbox/pillarbox applied to a UI Toolkit panel
- UI Toolkit preview
  - A Resources UI document is instantiated and wired: background set to the live RenderTexture, panel sized by aspect
- SRP render request (optional)
  - If supported, `RenderPipeline.SubmitRenderRequest` is used to request a frame into the target
- Global access
  - The active RenderTexture is exposed as `CameraRenderTextureHandler.RenderTexture`

## Requirements
- Unity 6000.0+
- A Camera component on the same GameObject (enforced by `[RequireComponent]`)
- Optional (UI preview): a `Resources` prefab named `UnityEssentials_Camera_UIDocument` containing two `UIElementLink` targets
  - Child names expected: `"VisualElement (AspectRatio)"` and `"VisualElement (RenderTexture)"`
- Optional (SRP request): a Scriptable Render Pipeline that supports `RenderPipeline.SubmitRenderRequest`

## Usage
1) Add to a camera
   - Add `CameraRenderTextureHandler` to a Camera
2) Configure settings
   - Size: `RenderWidth`/`RenderHeight` (validated to a minimum of 100×100)
   - Aspect: set `AspectRatioNumerator/Denominator` (0/0 means use current screen aspect)
   - Quality: `HighDynamicRange`, `UseMipMap`, and `FilterMode`
3) Update at runtime
   - Changes to settings mark the target as dirty; in Play Mode, the texture is recreated and the UI overlay is updated
   - Use the context menu action `Update` to apply changes immediately
4) Consume the texture
   - Access `CameraRenderTextureHandler.RenderTexture` from elsewhere
   - The camera’s `targetTexture` is set automatically to the managed texture

## How It Works
- Initialization
  - On Awake, a UI document is optionally instantiated from `Resources/UnityEssentials_Camera_UIDocument` and critical elements are cached
- Change detection
  - Settings (size, aspect, filter, HDR) and screen size changes are monitored; if anything changes, the texture/UI is refreshed
- RenderTexture lifecycle
  - The existing texture is released/destroyed, a new one is created with selected format, mipmap, and filter settings, and assigned to `camera.targetTexture`
- Aspect‑ratio preview
  - The handler computes a corrected aspect (screen or explicit) and previews the texture in a UI panel using width/height percentages for letterboxing
- Render request
  - When supported by the active SRP, a `StandardRequest` is submitted to draw once into the camera’s `targetTexture`; otherwise the regular camera render path fills it

## Notes and Limitations
- UI dependencies: The UI preview relies on a prefab named `UnityEssentials_Camera_UIDocument` with elements named `VisualElement (AspectRatio)` and `VisualElement (RenderTexture)`; without it, the RenderTexture still functions
- SRP compatibility: Render requests are used only when `RenderPipeline.SupportsRenderRequest` returns true; otherwise the camera’s normal rendering applies
- Performance: Recreating large HDR textures and generating mipmaps can be costly; avoid frequent size toggles at runtime
- Filtering/mipmaps: Filter mode and mipmaps affect sampling quality when the texture is used by materials/UI
- Static reference: `RenderTexture` is static and shared; be mindful when multiple handlers are used

## Files in This Package
- `Runtime/CameraRenderTextureHandler.cs` – RenderTexture creation/assignment, aspect‑ratio UI, SRP render request
- `Runtime/UnityEssentials.CameraRenderTextureHandler.asmdef` – Runtime assembly definition
- `Resources/UnityEssentials_Camera_UIDocument` – Optional UI Toolkit document prefab (expected child names as above)

## Tags
unity, camera, rendertexture, hdr, sdr, mipmap, filter, aspect‑ratio, letterbox, srp, render‑request, uitoolkit, runtime
