# Toolbox And Retouch Panel Contract

## Purpose

This document fixes the ownership boundary between the toolbox and the right retouch panel.

The toolbox is not only a visual toolbar.
It is the input and target-definition layer for the retouch engine.

The right retouch panel is not the target-definition layer.
It is the parameter dispatch layer.

## Core Rule

```text
Toolbox
-> target / work area / selection / mask / sample / stroke input
-> Retouch Panel
-> values / strength / mode / color / preset
-> Engine Request
-> Preview
-> History
```

## Toolbox Responsibility

The toolbox owns user intent that answers:

- where the work happens
- what pixels are allowed to be touched
- what shape, stroke, path, or sample defines the operation
- what coordinate space the request belongs to
- what mask or region should be attached to the operation

Examples:

- Rectangle selection defines a rectangular work region.
- Lasso defines a freehand selection mask.
- Magic selection defines a color-derived selection mask.
- Brush defines stroke points, brush size, softness, and stroke mask.
- Sampler defines sampled pixel position and averaged color evidence.
- Ruler defines measurement input.
- Path defines vector-like geometry.
- Crop defines a final image-space crop rectangle.

## Retouch Panel Responsibility

The right retouch panel owns user values that answer:

- how much
- what strength
- what radius
- what color
- what blend mode
- what preset
- which engine operation should run

Examples:

- Tone Correction sends curve, exposure, contrast, saturation, and sharpen values.
- Skin sends smoothing amount, texture amount, and protection values.
- Background sends background color, opacity, and blend values.
- Face Shape sends warp amount and balance values.

## Engine Request Shape

Every future retouch operation should be expressible as:

```text
OperationRequest =
    SourceImageState
    + TargetScope
    + ToolInput
    + RetouchParameters
    + PreviewPolicy
    + HistoryPolicy
```

Where:

- `SourceImageState` = current photo / original image / adjusted preview source.
- `TargetScope` = full image, work area, selection, mask, or local proxy.
- `ToolInput` = selection geometry, brush stroke, path, sample point, or crop rectangle.
- `RetouchParameters` = slider and option values from the right panel.
- `PreviewPolicy` = how to render temporary result.
- `HistoryPolicy` = when to commit undo/history state.

## Safety Rule

A retouch slider must not silently invent a hidden target.

If the operation needs a target, it must get one from:

- current tool selection
- current brush stroke
- current path or region
- an explicit detector result
- a documented default full-image target

If the target is automatic, the operation must name it clearly.

Examples:

- `SkinSmooth` may use `SkinSafeMask`.
- `BackgroundReplace` may use `BackgroundMask`.
- `DoubleChin` may use `DoubleChinWorkMask`.
- `ToneCorrection` may use full-image target by default.

## Work Mode Rule

Editing operations run only in `Work Mode`.

`Viewer Mode` and `Compare View` may allow navigation, zoom, pan, sampling, or passive viewing,
but must not create destructive edit commits.

## History Rule

Every completed destructive operation must create one history entry.

Preview-only movement, hover, measuring, and selection drawing must not create history entries until the operation is applied.

## Layer Future Rule

The current implementation may write to `AdjustedImage`.

Future implementation should allow the same operation to become a layer-backed operation without changing the toolbox/panel ownership boundary.

Therefore:

- toolbox defines target input
- retouch panel defines values
- engine request combines both
- history records the combined operation

