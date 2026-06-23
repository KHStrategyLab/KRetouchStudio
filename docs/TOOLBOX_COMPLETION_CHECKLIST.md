# KRetouchPro Toolbox Completion Checklist

## Purpose

This checklist keeps toolbox cleanup executable without reopening the whole design discussion.

Toolbox work must keep these rules:

- Editing tools run only in `Edit Mode`.
- `Viewer Mode` and `Multi Mode` remain viewer-first states.
- Every destructive pixel change must create a history snapshot.
- Selection tools create reusable masks or regions.
- Placeholder tools must clearly say what is complete and what is pending.
- Toolbox defines target input; right retouch panel dispatches values.

## Current Runtime Gate

- `CurrentRuntimeWorkMode = Viewer` when selected photo count is `0`.
- `CurrentRuntimeWorkMode = Viewer` when selected photo count is `1` and no right retouch tab is expanded.
- `CurrentRuntimeWorkMode = Edit` when selected photo count is `1` and one right retouch tab is expanded.
- `CurrentRuntimeWorkMode = Multi` when selected photo count is `2+`.
- `Multi Mode` blocks right-tab edit entry and save.
- Work-area/tether imports can auto-focus only in `Viewer Mode`.
- Edit history follows the normalized file path and is restored from local AppData on restart.
- Work-area refresh removes persisted history for files no longer present in the current folder.
- `CanUseSinglePreviewTool()` is the main edit safety gate.

## Tool Status

### Pixel Editing Tools

- [x] Brush / Pencil: writes pixels and commits history.
- [x] Eraser: restores original pixels and commits history.
- [x] Stamp: uses `Alt + Click` source and commits history.
- [x] Healing / Patch / Spot: applies baseline healing and commits history.
- [x] Blur / Sharpen: writes pixels and commits history.
- [x] Dodge / Burn: writes pixels and commits history.
- [x] Liquify: applies baseline warp, uses MediaPipe alpha tension when available, and commits history.
- [x] Fill / Gradient: writes pixels and commits history.
- [x] Type: creates editable text objects and commits history.

### Selection And Measurement Tools

- [x] Rectangle / Ellipse / Polygon: creates, moves, resizes, and applies regions.
- [x] Lasso: creates freehand selection geometry.
- [x] Magic / Quick Select: creates color-based selection overlay.
- [x] Path: creates path anchors and path geometry.
- [x] Path Selection / Direct Selection: moves path or anchors.
- [x] Ruler: measures distance and angle.
- [x] Sampler: calculates average pixel color over a configurable area.

### Navigation Tools

- [x] Hand: routes to single-preview pan.
- [x] Zoom click: click zooms in, `Alt` click zooms out.
- [x] Zoom drag box: zooms into the selected preview region.

### Cleanup Targets

- [x] Move Crop apply responsibility from `RectangleTool` into `CropTool`.
- [x] Keep Rectangle tool responsible for region creation only.
- [x] Add Zoom drag-box apply behavior.
- [x] Clarify Select as neutral/default view-control mode.
- [x] Clarify Frame as region-only until frame rendering is implemented.
- [x] Clarify Path as geometry-only until mask/shape conversion is implemented.
- [x] Document toolbox / retouch-panel ownership boundary.

## Ownership Boundary

Toolbox owns:

- work area
- selection
- mask source
- brush stroke
- path
- sample
- crop rectangle

Right retouch panel owns:

- slider values
- strength
- radius
- color
- opacity
- blend mode
- engine operation parameters

The retouch panel must send values into an explicit target.
It must not create hidden target areas without naming the detector or default target.

## Recommended Patch Order

1. Crop responsibility cleanup.
2. Zoom drag-box behavior.
3. Neutral/pending tool state text.
4. Build and verify.
5. Commit and push.
