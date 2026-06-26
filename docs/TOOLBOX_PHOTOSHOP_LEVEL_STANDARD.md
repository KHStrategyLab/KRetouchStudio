# Toolbox Photoshop-Level Standard

This document defines the long-term quality target for KRetouchStudio toolbox tools.

Retouch tabs are automatic or semi-automatic engines. Toolbox tools are manual precision tools. They must feel predictable, fast, and accurate enough for daily studio work.

## Core Principle

Toolbox tools must behave like professional photo-editing tools:

- The cursor preview must match the actual affected pixels.
- Dragging must feel continuous and stable.
- Each stroke or operation must have a clean undo unit.
- Large images must update only the required region when possible.
- Tool state must stay predictable across zoom, pan, selection, and photo changes.

## Functional Target

The functional target for every toolbox tool is Adobe Photoshop CS3.

This means each tool should aim for the core behavior, predictability, shortcut flow, and manual editing feel that a Photoshop CS3 user would expect. KRetouchStudio may add, remove, simplify, or rename options when that better fits studio retouching workflow, performance, or the current UI structure.

Photoshop CS3 is the behavioral reference, not a requirement to copy every option one-to-one.

## Tool Groups

| Group | Tools | Main target |
| --- | --- | --- |
| Paint | Brush, Eraser, History Brush | smooth stroke, correct softness, stroke-level undo |
| Repair | Healing, Stamp | source/target control, texture-safe repair, predictable sampling |
| Shape | Liquify, Transform, Crop | stable geometry edit, ROI updates, non-jitter preview |
| Selection | Rectangle, Lasso, Magic, Path | accurate mask/selection creation and reuse |
| Tone Local | Blur/Sharpen, Dodge/Burn | local strength control, no banding, stroke-level undo |
| Annotation | Type, Shape, Ruler | precise placement, stable transform, clear state |

## Required Behavior

### 1. Cursor And Coordinate Accuracy

- Tool cursor size must match the affected image-space radius.
- Preview coordinates must stay correct at every zoom level.
- Pan/zoom must not change the image-space target point.
- Cursor preview must hide when the tool cannot apply.

### 2. Stroke Pipeline

Every stroke-based tool should follow this sequence:

```text
Pointer down
-> validate photo/tool/source
-> create stroke session
-> capture stroke base state
-> apply dab/segment by ROI
-> update display
-> pointer up
-> commit one history snapshot
```

Rules:

- Do not push history per dab.
- Do not rebuild the full image per pointer move unless the tool cannot work by ROI.
- If the pointer leaves the preview surface, finish or pause the stroke predictably.
- Stroke cancellation should restore the stroke base state.

### 3. Undo And History

- One stroke equals one undo entry.
- One transform apply equals one undo entry.
- Selection edits should not pollute image history unless pixels are changed.
- Tool reset operations must have clear history behavior.

### 4. Performance

- Brush, eraser, healing, stamp, blur/sharpen, dodge/burn, and liquify should update only the dirty ROI.
- Heavy operations must not run on the UI thread during pointer move.
- Expensive mask or texture preparation must be cached.
- A tool must never start multiple full-image operations from continuous pointer events.

### 5. Modifier Keys

Use standard editing semantics where possible:

- `Alt`: sample source for stamp/healing or alternate picker behavior.
- `Shift`: constrain direction, line, or aspect where relevant.
- `Ctrl`: selection modification or transform precision where relevant.
- `Esc`: cancel active operation when safe.
- `Enter`: apply active transform/text operation.

### 6. Selection And Mask Integration

- Paint and repair tools must respect active selection/mask when enabled.
- Future skin cleanup should reuse skin-safe masks rather than recomputing per stroke.
- Selection tools should produce reusable mask artifacts.
- Mask overlays must be optional and non-destructive.

### 7. Tool State

- Tool size, softness, strength, source point, and mode should remain stable while the same photo is active.
- Photo change must clear unsafe state such as stamp/healing source and active stroke sessions.
- Tool switch must end active drag operations safely.
- App close must not leave worker or render operations running.

## Tool-Specific Targets

### Brush / Eraser

- Smooth continuous stroke.
- Softness curve should be visually predictable.
- Brush preview and actual edge must match.
- Eraser must respect opacity/softness and active selection.

### Healing

- `Alt` source pick.
- Source and target must remain visually understandable.
- Heal should preserve local tone and texture.
- One stroke should produce one history entry.
- Future target: patch-based texture blend with skin mask awareness.

### Stamp

- `Alt` source pick.
- Source offset must stay stable during stroke.
- Aligned/non-aligned mode should be considered.
- Source preview overlay is recommended for precision work.

### Liquify

- ROI update only.
- No full-image recompute per pointer move.
- Tension/mask maps must be cached.
- Reset and apply must be clear history operations.
- Future target: brush modes such as push, pull, smooth, bloat, pinch.

### Selection Tools

- Rectangle/Lasso/Magic/Path must create reusable selection masks.
- Selection edges should support feather/expand/contract.
- Selection must be usable by retouch tabs and toolbox tools.
- Future target: subject/skin/hair masks can be converted into editable selections.

### Blur/Sharpen And Dodge/Burn

- Stroke-level undo.
- Strength must be bounded to avoid banding or clipping.
- Should respect selection/mask.
- Future target: pressure-like falloff and tonal range targeting.

### Type / Shape / Ruler

- Text edit state must be explicit.
- Apply/cancel behavior must be predictable.
- Transform handles should remain stable under zoom.
- Ruler measurements should use image-space coordinates.

## Daily Workflow Priority

Upgrade order for studio use:

1. Healing
2. Stamp
3. Brush / Eraser
4. Liquify
5. Selection / Mask integration
6. Blur/Sharpen and Dodge/Burn
7. Type / Shape polish

## Review Checklist

Before a toolbox upgrade is accepted:

- [ ] Cursor preview matches affected image-space area.
- [ ] Zoom/pan does not shift the target.
- [ ] One stroke creates one history entry.
- [ ] Pointer move does not run full-image work unnecessarily.
- [ ] Tool respects selection/mask where expected.
- [ ] Tool state resets safely on photo change.
- [ ] App remains responsive during repeated strokes.
- [ ] x64 build passes.
- [ ] Project director performs visual confirmation.

## Patch Order Standard

For toolbox upgrades:

1. Audit one tool only.
2. Identify the current stroke flow.
3. Fix cursor/coordinate mismatch first.
4. Fix history granularity second.
5. Fix ROI/performance third.
6. Add mask/selection integration after the tool is stable.
7. Build x64 and visually verify.

Do not redesign multiple toolbox tools in one patch unless explicitly approved.
