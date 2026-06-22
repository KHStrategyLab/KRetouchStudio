# LOCAL_PROXY_WORKBENCH_DESIGN.md

Last updated: 2026-06-18

## Purpose

This document defines the local proxy workbench design for KRetouchPro.

This revision is aligned to the current source implementation in:

- `Tools/PhotoAdjustment/LocalWorkbench/LocalProxyBuilder.cs`
- `Tools/PhotoAdjustment/LocalWorkbench/LocalWorkbenchModels.cs`
- `MainWindow.xaml.cs`

The workbench is a Photoshop CS3-style local preview workspace inside the main preview screen.

The goal is to let the engine open the exact requested work area, render a fast local proxy, let the user adjust the amount, and then apply the confirmed result back to the main preview.

Current source checkpoint:

- Workbench open is connected for `DoubleChin` and `Nose Shape`.
- The current workbench is display-first:
  - crop original image
  - build proxy
  - show overlay
  - show placeholder mask layers
- Slider-driven local preview is not connected yet.
- `Apply` and `Cancel` currently close the overlay only.

---

# 1. Core Idea

A user request starts from intent.

The engine translates that intent into:

- WorkArea
- ProxyPolicy
- ApplyMask
- ProtectMask
- BlockMask
- Amount
- LocalPreview
- ApplyToMainPreview

Main rule:

```text
UserIntent
-> WorkArea
-> LocalProxy
-> LocalMask
-> LocalEdit
-> Apply
-> MainPreview
```

The main preview shows the whole photo.
The local proxy workbench shows the working area.

---

# 2. Visual Structure

## Main Preview

The main preview remains the full image view.

It shows:

- current full photo
- current global retouch result
- background replacement result
- applied local edits

## Local Proxy Workbench Overlay

The local proxy workbench appears inside the main preview area.

It shows:

- cropped work area
- enlarged proxy preview
- active mask overlay
- protected regions
- blocked regions
- final work-mask overlay
- Apply button
- Cancel button

The workbench is a temporary editing workspace for the selected tool.

Current source note:

- The current overlay does not yet provide:
  - local amount slider
  - before/after toggle
  - final original-image apply

---

# 3. WorkArea First Flow

Every local tool begins by building a WorkArea from the original image coordinate space.

```text
OriginalImage
-> WorkAreaBuilder
-> WorkAreaRect
-> WorkAreaCrop
-> LocalProxy
```

The WorkArea is chosen by the tool purpose.

Examples of WorkArea ownership:

- DoubleChin tool: chin center, jawline, upper neck
- Eye tool: left eye or right eye area
- Ear tool: left ear or right ear area
- Lip tool: mouth and lip area
- HairColor tool: hair region or head upper region
- Hand tool: left hand or right hand area
- BackgroundReplace tool: full image frame

---

# 4. ProxyPolicy

Each tool defines its proxy policy.

## FullFrameProxy

Used when the whole visible person silhouette is required.

Typical tools:

- background replacement
- full person extraction
- global subject alpha check

## LocalWorkAreaProxy

Used when only one region needs calculation.

Typical tools:

- double chin
- eye
- ear
- lips
- nose
- hand
- scar
- tattoo
- mole
- wrinkle
- hairline
- gray hair coverage

## Proxy Size

The current source does not use the earlier `256 / 640 / 1200` execution note as-is.

### Active Source Levels

- `1200 = FullFrameProxy`
  - selected when `ProxyPolicy == FullFrameProxy`
  - intended for full-frame work

- `384 = LocalWorkAreaProxy bucket A`
  - selected when local work-area long side is `<= 384`

- `512 = LocalWorkAreaProxy bucket B`
  - selected when local work-area long side is `<= 512`

- `768 = LocalWorkAreaProxy bucket C`
  - selected when local work-area long side is `<= 768`

- `1024 = LocalWorkAreaProxy bucket D`
  - selected when local work-area long side is `> 768`

### Source Rule

Current source behavior:

```text
if FullFrameProxy -> 1200
else
    longSide <= 384 -> 384
    longSide <= 512 -> 512
    longSide <= 768 -> 768
    else            -> 1024
```

### Design Notes Kept For Later

- `256` remains a useful future precision-proxy idea.
- `640` remains a useful future interactive-standard idea.
- They are not the current active sizing policy in source.

### Important Rule

The proxy is still aspect-ratio preserving.

The correct interpretation is:

- preserve the source aspect ratio
- map the `WorkAreaCrop` into the working proxy space
- keep all authoritative coordinates in original-image space

### Current Active Paths

Global path:

```text
OriginalImage
-> 1200 FullFrame Proxy
-> Global Mask
-> Full Preview
-> Original Apply
-> MainPreview
```

Local path:

```text
OriginalImage
-> WorkAreaRectOriginal
-> WorkAreaCrop
-> Bucketed Local Proxy (384 / 512 / 768 / 1024)
-> Placeholder Mask Overlay
-> Display-Only Workbench Overlay
```

Current source note:

- `DoubleChin` and `Nose Shape` already open through this local path.
- The current path stops at overlay display.
- There is no connected `LocalEditDelta` or original-image apply stage yet.

### Mask Generation Rule

Current source behavior on workbench open:

- build placeholder mask overlays by tool id
- `double_chin` uses double-chin placeholder mask visuals
- `nose_shape` uses nose placeholder mask visuals

Planned later behavior:

- replace placeholder overlays with detector-backed masks
- add precision-entry refinement only when the preview/render stage is connected

---

# 5. Coordinate Mapping

The workbench keeps a mapping between original image coordinates and proxy coordinates.

Required coordinate objects:

- OriginalImageRect
- WorkAreaRectOriginal
- WorkAreaProxySize
- OriginalToProxyTransform
- ProxyToOriginalTransform
- MainPreviewTransform

Each mask and edit result produced inside the local proxy must be mapped back through `ProxyToOriginalTransform`.

---

# 6. Workbench Request Object

Current source request object:

```text
LocalWorkbenchRequest
- ToolId
- UserIntent
- SourcePhotoId
- WorkAreaRectOriginal
- ProxyPolicy
- ProxyLongSide
- ApplyMaskId
- ProtectMaskIds
- BlockMaskIds
- InitialAmount
- CurrentAmount
```

---

# 7. Workbench State Object

Current source state object:

```text
LocalWorkbenchState
- Request
- CoordinateMap
- WorkAreaCropSource
- LocalProxySource
- LocalMaskSet
- CurrentAmount
- IsDirty
- IsApplied
- IsCanceled
```

---

# 8. Tool Specification

Every slider-based local tool should have a tool specification.

Recommended fields:

```text
SliderToolSpec
- ToolId
- DisplayName
- Intent
- WorkAreaBuilder
- ProxyPolicy
- ApplyMaskBuilder
- ProtectMaskBuilder
- BlockMaskBuilder
- AmountRange
- DefaultAmount
- PreviewRenderer
- ApplyRenderer
```

This makes every slider predictable.

The user controls the amount.
The engine controls the area, masks, protection, and coordinate mapping.

---

# 9. Mask Routing

Each tool produces a final WorkMask.

```text
WorkMask = ApplyMask * (1 - ProtectMask) * (1 - BlockMask)
```

Workbench displays:

- ApplyMask as active area
- ProtectMask as protected area
- BlockMask as excluded area
- WorkMask as final editable area

---

# 10. Local Preview Rendering

The local preview renderer should eventually run only inside the WorkArea proxy.

Flow:

```text
LocalProxySource
+ LocalMaskSet
+ CurrentAmount
-> LocalPreviewResult
```

Current source status:

- a separate local preview renderer is not connected yet
- the displayed image is currently `LocalProxySource`
- main preview remains unchanged while the workbench is open

---

# 11. Apply Flow

Planned target when the user presses Apply:

```text
LocalPreviewResult
-> Map result to original coordinate space
-> Update current PhotoRetouchState
-> Re-render main preview from original/proxy source
```

Current source status:

```text
Apply
-> close LocalWorkbench
-> clear local workbench images
-> clear guide rectangle
```

No photo-state mutation is connected yet.

---

# 12. Cancel Flow

Current source behavior when the user presses Cancel:

```text
Close LocalWorkbench
-> clear local workbench images
-> clear guide rectangle
-> keep current photo state unchanged
-> keep main preview unchanged
```

---

# 13. Before / After in Workbench

The workbench should later support local before/after.

Before:

```text
WorkAreaProxySource
```

After:

```text
LocalPreviewResult
```

Current source status:

- before/after toggle is not connected yet

---

# 14. Main Preview Relationship

The workbench is a temporary layer above the main preview.

Main preview responsibilities:

- show full image
- show applied result
- provide visual context

Workbench responsibilities:

- show local area
- show masks
- show local preview
- collect Apply or Cancel

---

# 15. First Implementation Target

First target: display-only local proxy workbench.

Required output:

```text
Selected tool
-> build WorkArea
-> crop original image
-> build LocalProxy
-> show workbench overlay
```

Current source status:

- this target is implemented

---

# 16. Second Implementation Target

Second target: local preview slider.

Required output:

```text
LocalProxy
+ WorkMask
+ Amount
-> LocalPreviewResult
```

Current source status:

- this target is not implemented yet

---

# 17. Third Implementation Target

Third target: Apply to main preview.

Required output:

```text
LocalPreviewResult
-> original coordinate mapping
-> PhotoRetouchState update
-> main preview re-render
```

Current source status:

- this target is not implemented yet

---

# 18. Fourth Implementation Target

Fourth target: connect real tool entry points.

Current connected tool entries:

```text
DoubleChin
NoseShape
```

Reason:

- clear WorkArea
- clear ApplyMask
- clear ProtectMask
- visible result
- good proxy benefit

DoubleChin tool routing:

```text
UserIntent: DoubleChin
WorkArea: chin center + jawline + upper neck
ApplyMask: DoubleChinMask
ProtectMask: JawlineMask + BeardMask
BlockMask: ClothingMask + AccessoryMask
Amount: user slider
ProxyPolicy: LocalWorkAreaProxy
```

---

# 19. Workbench Tool Examples

## HairColor

```text
WorkArea: head / hair region
ApplyMask: HairMask
ProtectMask: FaceSkinMask + EarMask
BlockMask: EyebrowMask + EyelashMask + FacialHairMask + AccessoryMask
ProxyPolicy: LocalWorkAreaProxy
```

## EarScale

```text
WorkArea: left ear or right ear
ApplyMask: LeftEarMask or RightEarMask
ProtectMask: HairMask + FaceRegionMask
BlockMask: NeckBioMask
ProxyPolicy: LocalWorkAreaProxy
```

## BackgroundReplace

```text
WorkArea: full image
ApplyMask: BackgroundMask
ProtectMask: PersonAlpha + HairAlpha
BlockMask: PersonMask
ProxyPolicy: FullFrameProxy
```

## NoseShape

```text
WorkArea: nose bridge + tip + alar + upper philtrum entry
ApplyMask: NoseMask
ProtectMask: LeftEyeMask + RightEyeMask + LipMask
BlockMask: GlassesMask + BeardMask
ProxyPolicy: LocalWorkAreaProxy
```

---

# 20. UI Placement

The workbench appears centered over the main preview.

Recommended layout:

```text
[Local Proxy Preview]
[ApplyMask / ProtectMask / BlockMask / WorkMask overlays]
[Apply] [Cancel]
```

The overlay may dim the surrounding main preview while active.

---

# 21. Data Ownership

The source of truth remains:

```text
OriginalImage + CurrentPhotoRetouchState
```

The workbench owns temporary local preview data only while open.

The applied result is stored as state, not as a preview bitmap.

---

# 22. Build Direction

Current source-aligned file direction:

```text
Tools/PhotoAdjustment/LocalWorkbench/
- LocalWorkbenchModels.cs
- LocalProxyBuilder.cs
- LocalWorkbenchMaskPreviewBuilder.cs
- DoubleChinWorkAreaBuilder.cs
- NoseWorkAreaBuilder.cs
```

UI direction:

```text
MainWindow.xaml
- LocalWorkbenchOverlay
- LocalProxyImage
- Apply / Cancel controls
```

Current source orchestration direction:

```text
OpenDoubleChinWorkbench_Click()
OpenNoseWorkbench_Click()
LocalProxyBuilder.BuildDisplayOnlyState(...)
ApplyWorkbench_Click()
CancelWorkbench_Click()
```

---

# 23. Success Condition

Current successful version:

```text
Open DoubleChin tool
-> local proxy workbench appears over main preview
-> correct chin work area is shown
-> main preview stays stable
-> Apply/Cancel buttons exist
```

Additional current successful version:

```text
Open Nose Shape tool
-> local proxy workbench appears over main preview
-> nose work box is used when detection context exists
-> fallback nose work area is used when detection context is unavailable
```

Later successful version:

```text
Slider changes only local proxy preview
```

Later successful version:

```text
Apply updates the full main preview through PhotoRetouchState
```

---

# 24. Final Direction

The local proxy workbench still aims to turn each retouch slider into a structured request.

The engine prepares:

- where to work
- what mask to apply
- what to protect
- what to block
- what proxy to use

The user currently decides:

- whether to open the tool
- whether to close it with Apply or Cancel

The user later decides:

- whether to use the tool
- how much to apply
- whether to apply or cancel

This keeps the program as a controlled retouch engine, not an automatic beauty filter.
