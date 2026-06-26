# Face Detail Retouch Slider Contract

## Purpose

This document defines the connection contract for the `Face Detail` retouch panel.
It converts the UI sliders into engine-facing operation names, target scopes, masks, preview policy, and implementation order.

The goal is to make the next implementation pass direct:

```text
FaceDetailTabView
-> FaceDetailRetouchParameters
-> FaceDetailRetouchRequest
-> local work area / masks / landmarks
-> proxy preview
-> serialized full-resolution commit
-> history entry
```

## Source Documents

Use this document together with:

- `TOOLBOX_RETOUCH_PANEL_CONTRACT.md`
- `DAILY_RETOUCH_STABILITY_STANDARD.md`
- `ANCHOR_REGION_CROP_RETOUCH_LAYER.md`
- `DETECTOR_TARGET_AND_MASK_IDS.md`
- `PERSON_MASK_AND_SUBJECT_FORMULA_SPEC.md`
- `CORE_FORMULA_COMPANION.md`
- `BUILD_DOUBLE_CHIN_MASK_CONTRACT.md`
- `BUILD_NECK_WRINKLE_MASK_CONTRACT.md`

## Ownership Boundary

The right retouch panel owns parameter dispatch only.
It must not silently invent a hidden target.

Each Face Detail slider must resolve:

- `OperationId`
- `Strength`
- `SideMode`
- `TargetWorkBox`
- `ApplyMask`
- `ProtectMask`
- `PreviewPolicy`
- `HistoryPolicy`

Target resolution must come from:

- cached landmarks
- documented detector targets
- documented masks
- documented default local work boxes

## UI Layout Contract

The panel uses a nested structure:

```text
Face Detail expander
-> Eyes / Brows / Nose / Mouth / Neck horizontal tabs
-> single sliders first
-> linked pair sliders below
```

Single sliders stay one-line:

```text
Label  [ slider ]
```

Linked pair sliders use two rows:

```text
Label
L [ slider ]  [link / link-slash]  R [ slider ]
```

The link toggle is UI-only coupling.
The engine must still receive explicit left and right values.

## Strength Rule

Current sliders are unsigned:

```text
0   = no change
100 = maximum approved studio correction
```

Do not interpret these sliders as `-100..100`.
If a future tool needs reverse direction, add a signed slider or a direction option explicitly.

For implementation, normalize:

```text
Amount = SliderValue / 100.0
```

Each operation then maps `Amount` to its own conservative maximum.
Preview and commit must use the same mapping.

## Preview And Commit Rule

Face Detail sliders are high-use retouch controls.
They must follow the daily-use stability standard.

Preview:

- proxy only
- latest result wins
- no history write
- no uncached AI/Python/GPU work per slider tick
- local work area only

Commit:

- serialized per photo
- full-resolution allowed
- version checked before result assignment
- one history entry per committed operation group

## Request Shape

Use one shared request shape for all Face Detail sliders:

```text
FaceDetailRetouchRequest =
    PhotoId
    SourceImageVersion
    PreviewOrCommit
    ActiveTab
    OperationId
    LeftAmount
    RightAmount
    GlobalAmount
    WorkBoxId
    ApplyMaskIds
    ProtectMaskIds
    RenderScale
```

For single sliders:

```text
GlobalAmount = value
LeftAmount   = value
RightAmount  = value
```

For pair sliders:

```text
GlobalAmount = max(LeftAmount, RightAmount)
LeftAmount   = left slider value
RightAmount  = right slider value
```

## Common Protect Masks

Use these masks unless an operation gives a stricter rule.

Eyes:

```text
ProtectMask = EyebrowMask + EyelashMask + GlassesMask
```

Brows:

```text
ProtectMask = EyeMask + EyelashMask + GlassesMask + FaceSkinMask
```

Nose:

```text
ProtectMask = EyeMask + LipMask + MouthInnerMask + GlassesMask
```

Mouth:

```text
ProtectMask = ToothMask + MouthInnerMask + FaceSkinMask + FacialHairMask
```

Neck:

```text
ProtectMask = ClothingMask + AccessoryMask + BeardMask + HairMask
```

## Slider Inventory

### Eyes

| UI label | OperationId | Type | Target | ApplyMask | Rule status |
| --- | --- | --- | --- | --- | --- |
| Eye Size | `EyeOverallScale` | warp | `eye_work_box` | `LeftEyeMask + RightEyeMask` | ready |
| Distance | `EyeDistanceBalance` | warp | `eye_work_box` | `LeftEyeMask + RightEyeMask` | ready with auto-target rule |
| Height | `EyeHeightOpen` | pair warp | left/right `eye_work_box` | `LeftEyeMask`, `RightEyeMask` | ready |
| Width | `EyeWidthStretch` | pair warp | left/right `eye_work_box` | `LeftEyeMask`, `RightEyeMask` | ready |
| Tilt | `EyeCornerTilt` | pair warp | left/right outer eye ROI | `LeftEyeMask`, `RightEyeMask` | ready |
| Dark | `DarkCircleReduce` | pair tone | left/right under-eye ROI | `DarkCircleMask` | ready |
| Under Wrinkle | `UnderEyeWrinkleSoften` | pair texture | left/right under-eye ROI | `UndereyeMask` refined by wrinkle evidence | needs new mask builder or fallback |

#### `EyeOverallScale`

Reference:

- `CORE_FORMULA_COMPANION.md` formula 34
- `PERSON_MASK_AND_SUBJECT_FORMULA_SPEC.md` eye retouch work mask

Rule:

```text
WorkBox = left_eye_work_box + right_eye_work_box
ApplyMask = EyeRetouchWorkMask
Amount = GlobalAmount
```

Use radial bloat/shrink around each eye center.
Current unsigned slider means studio-positive enlargement only.

#### `EyeDistanceBalance`

Rule:

```text
TargetRatio = documented face-average eye spacing ratio
Delta = (TargetRatio - CurrentRatio) * Amount
```

Move both eye work boxes symmetrically around the facial midline.
If the ratio cannot be measured, no-op instead of guessing.

This operation is a one-way auto-balance, not a manual signed expand/contract slider.

#### `EyeHeightOpen`

Rule:

```text
UpperEyelid moves upward by Amount
LowerEyelid moves downward by Amount * 0.35
Eye corners remain damped
```

Use eyelid curve targets from `DETECTOR_TARGET_AND_MASK_IDS.md`.
Protect brows and lashes.

#### `EyeWidthStretch`

Reference:

- `CORE_FORMULA_COMPANION.md` formula 35

Rule:

```text
Horizontal stretch only
Vertical movement = 0
```

Use per-side values.

#### `EyeCornerTilt`

Reference:

- `CORE_FORMULA_COMPANION.md` formula 37

Rule:

```text
Inner corner anchor = strong
Outer corner = rotated by Amount
```

Positive amount applies the approved studio lift direction.

#### `DarkCircleReduce`

Reference:

- `PERSON_MASK_AND_SUBJECT_FORMULA_SPEC.md` `DarkCircleMask`

Rule:

```text
ApplyMask = UnderEyeROI * SkinMask * DarkCircleProb
Effect = lift shadow tone + reduce local blue/purple cast
```

No geometry warp.

#### `UnderEyeWrinkleSoften`

Minimum rule for implementation:

```text
UnderEyeWrinkleMask =
    UnderEyeROI
    * SkinMask
    * WrinkleProb
    * (1 - EyeMask)
    * (1 - EyelashMask)
```

Effect:

```text
texture soften only
preserve lower eyelid edge
preserve iris/sclera
```

If `WrinkleProb` is not available, use a conservative high-frequency texture mask inside the under-eye ROI.

### Brows

| UI label | OperationId | Type | Target | ApplyMask | Rule status |
| --- | --- | --- | --- | --- | --- |
| Distance | `BrowDistanceBalance` | warp | brow work box | `EyebrowMask` | ready with auto-target rule |
| Thickness | `BrowThicknessAdjust` | pair mask/warp | left/right brow ROI | `LeftEyebrowMask`, `RightEyebrowMask` | ready |
| Tilt | `BrowTiltAdjust` | pair warp | left/right brow ROI | `LeftEyebrowMask`, `RightEyebrowMask` | ready |
| Arch | `BrowArchAdjust` | pair warp | brow body/tail | `LeftEyebrowMask`, `RightEyebrowMask` | ready |
| Position | `BrowPositionLift` | pair warp | left/right brow ROI | `LeftEyebrowMask`, `RightEyebrowMask` | ready |
| Tail | `BrowTailAdjust` | pair warp | brow tail ROI | `LeftEyebrowMask`, `RightEyebrowMask` | ready |

Reference:

- `CORE_FORMULA_COMPANION.md` formula 17
- `DETECTOR_TARGET_AND_MASK_IDS.md` brow and eye targets
- `PERSON_MASK_AND_SUBJECT_FORMULA_SPEC.md` `EyebrowMask`

General brow rule:

```text
Mask_brow = distance-to-brow-curve band
ProtectMask = EyeMask + EyelashMask + GlassesMask + FaceSkinMask
```

#### `BrowDistanceBalance`

One-way auto-balance toward a studio target gap.
No signed manual move until the UI supports signed values.

#### `BrowThicknessAdjust`

Use a curve-normal expansion around the eyebrow body.

```text
BrowThicknessTarget = BaseThickness * (1 + Amount * MaxThicknessGain)
```

Do not paint outside the brow work box.

#### `BrowTiltAdjust`

Rotate brow body around brow head.
Keep brow head damped to avoid moving the inner brow into the nose bridge.

#### `BrowArchAdjust`

Move brow body peak along the local normal.
Use brow head and brow tail as damped anchors.

#### `BrowPositionLift`

Translate the whole brow band in the approved upward studio direction.
Do not move eyelids.

#### `BrowTailAdjust`

Move the lateral brow tail only.
Keep brow head/body damped.

### Nose

| UI label | OperationId | Type | Target | ApplyMask | Rule status |
| --- | --- | --- | --- | --- | --- |
| Size | `NoseSizeRefine` | warp | `nose_work_box` | `NoseMask` | ready |
| Length | `NoseLengthRefine` | warp | nose axis ROI | `NoseMask` | ready |
| Bridge | `NoseBridgeRefine` | tone/warp | bridge band | `NoseBridgeMask` | ready |
| Width | `NoseWidthRefine` | warp | side planes | `NoseMask` | ready |
| Tip | `NoseTipRefine` | warp/tone | tip mass | `NoseTipMask` | ready |
| Nostril | `NostrilBalance` | pair warp | left/right alar ROI | `LeftNostrilMask`, `RightNostrilMask` | ready |

Reference:

- `DETECTOR_TARGET_AND_MASK_IDS.md` nose targets
- `CORE_FORMULA_COMPANION.md` formula 38

General nose rule:

```text
WorkBox = nose_work_box
ProtectMask = EyeMask + LipMask + MouthInnerMask + GlassesMask
TipAnchor = NoseTipMask damped unless operation is Tip
PhiltrumAnchor = PhiltrumMask damped
```

#### `NoseSizeRefine`

One-way studio refinement.
Use center-axis constrained compression.
Do not flatten the bridge or drag the mouth.

#### `NoseLengthRefine`

Move lower nose mass along the nose bridge axis toward the studio target.
Keep bridge root and philtrum damped.

#### `NoseBridgeRefine`

Prefer tone and local depth shading before warp.
If warp is used, it must stay inside `NoseBridgeMask`.

#### `NoseWidthRefine`

Compress left and right nose side planes toward the nose axis.
Damp the tip and philtrum.

#### `NoseTipRefine`

Small local mass refinement around `NoseTipMask`.
Do not reuse nostril compression as tip editing.

#### `NostrilBalance`

Reference:

- `CORE_FORMULA_COMPANION.md` formula 38

Use per-side alar/nostril correction.
Tip and philtrum must be protected.

### Mouth

| UI label | OperationId | Type | Target | ApplyMask | Rule status |
| --- | --- | --- | --- | --- | --- |
| Size | `MouthSizeRefine` | warp | `mouth_work_box` | `LipMask` | ready |
| Width | `MouthWidthRefine` | warp | mouth span | `LipMask` | ready |
| Vertical | `MouthVerticalRefine` | warp | lips | `LipMask` | ready |
| Upper Lip | `UpperLipThickness` | warp | upper lip | `UpperLipMask` | ready |
| Lower Lip | `LowerLipThickness` | warp | lower lip | `LowerLipMask` | ready |
| Corner | `MouthCornerLift` | pair warp | mouth corners | left/right corner ROI | ready |
| Smile | `SmileBalance` | pair warp | mouth corners | left/right corner ROI | ready |

Reference:

- `PERSON_MASK_AND_SUBJECT_FORMULA_SPEC.md` lip retouch work mask
- `CORE_FORMULA_COMPANION.md` formulas 39, 40, 41, 42
- `DETECTOR_TARGET_AND_MASK_IDS.md` mouth targets

General mouth rule:

```text
WorkBox = mouth_work_box
ApplyMask = LipRetouchWorkMask
ProtectMask = ToothMask + MouthInnerMask + FaceSkinMask + FacialHairMask
```

#### `MouthSizeRefine`

Use local scale around mouth center.
Damp teeth and mouth inner area.

#### `MouthWidthRefine`

Use horizontal-only mouth span refinement.
Keep vertical lip thickness damped.

#### `MouthVerticalRefine`

Use vertical-only lip opening/plump refinement.
Keep mouth corners damped unless corner operations are active.

#### `UpperLipThickness` / `LowerLipThickness`

Reference:

- `CORE_FORMULA_COMPANION.md` formula 41

Use upper/lower masks split by lip midline.
Do not move teeth or inner mouth.

#### `MouthCornerLift`

Reference:

- `CORE_FORMULA_COMPANION.md` formula 42

Use local corner lift.
A linked pair applies the same lift to both corners.
An unlinked pair can correct asymmetric corners.

#### `SmileBalance`

Use the same corner ROI as `MouthCornerLift`, but allow a broader cheek-mouth falloff.
This operation should be visually softer than `MouthCornerLift`.

### Neck

| UI label | OperationId | Type | Target | ApplyMask | Rule status |
| --- | --- | --- | --- | --- | --- |
| Slim | `NeckSlimRefine` | warp | `neck_work_box` | `NeckMask` / `NeckBioMask` | ready with conservative limits |
| Length | `NeckLengthRefine` | warp | neck axis ROI | `NeckMask` / `NeckBioMask` | ready with conservative limits |
| Wrinkle | `NeckWrinkleSoften` | texture | neck wrinkle bands | `NeckWrinkleMask` | ready |
| Double Chin | `DoubleChinReduce` | tone/warp | under jaw | `DoubleChinWorkMask` | ready |
| Side | `SideNeckBalance` | pair warp | left/right side neck | side neck ROI | ready |
| Shoulder | `ShoulderNeckBalance` | pair warp | shoulder-neck area | `ShoulderHumpMask` / `ShoulderMask` | ready |

Reference:

- `BUILD_DOUBLE_CHIN_MASK_CONTRACT.md`
- `BUILD_NECK_WRINKLE_MASK_CONTRACT.md`
- `PERSON_MASK_AND_SUBJECT_FORMULA_SPEC.md` lower-face and neck masks
- `PORTRAIT_BODY_HAIR_CLOTHING_LOCATION_DICTIONARY.md`

General neck rule:

```text
WorkBox = neck_work_box
ProtectMask = ClothingMask + AccessoryMask + BeardMask + HairMask
```

#### `NeckSlimRefine`

Move side neck boundaries inward toward the neck axis.
Use low maximum displacement.
Do not cross the jawline or clothing boundary.

#### `NeckLengthRefine`

Refine apparent neck length by moving the shoulder/clothing boundary support, not by dragging the face.
If clothing boundary is not available, no-op.

#### `NeckWrinkleSoften`

Reference:

- `BUILD_NECK_WRINKLE_MASK_CONTRACT.md`

Texture-only softening inside `NeckWrinkleMask`.
Do not treat the whole neck skin as wrinkle.

#### `DoubleChinReduce`

Reference:

- `BUILD_DOUBLE_CHIN_MASK_CONTRACT.md`

Use `DoubleChinWorkMask`.
Prefer tone/shadow cleanup first, then conservative shape support.

#### `SideNeckBalance`

Pair operation for side-neck asymmetry.
Use left/right side neck ROIs and protect clothing/hair.

#### `ShoulderNeckBalance`

Use `ShoulderHumpMask` or shoulder-neck work area.
Do not confuse shoulder-neck balance with the shoulder joint or clothing shoulder seam.

## Implementation Phases

### Phase 1 - Parameter Dispatch Shell

Implement:

- `FaceDetailRetouchParameters`
- `FaceDetailOperationId`
- `FaceDetailRetouchRequest`
- `FaceDetailTabView` change events
- slider debounce
- preview version token
- commit serialization gate

No image processing is required in this phase.

### Phase 2 - Mask And Work Area Resolution

Implement or reuse:

- eye work boxes
- brow work boxes
- nose work box
- mouth work box
- neck work box
- mask lookup by `MaskId`
- fallback no-op when required masks are missing

### Phase 3 - Low-Risk Texture/Tone Operations

Connect first:

- `DarkCircleReduce`
- `UnderEyeWrinkleSoften`
- `NeckWrinkleSoften`
- `DoubleChinReduce` tone pass

These should not warp facial geometry.

### Phase 4 - Landmark-Based Local Warp Operations

Connect next:

- `EyeOverallScale`
- `EyeHeightOpen`
- `EyeWidthStretch`
- `EyeCornerTilt`
- `UpperLipThickness`
- `LowerLipThickness`
- `MouthCornerLift`
- `NostrilBalance`

### Phase 5 - Conservative Shape Refinement

Connect after visual confirmation:

- brow operations
- nose size/length/bridge/width/tip
- mouth size/width/vertical
- neck slim/length/side/shoulder

### Phase 6 - Optimization

Only after Phase 3-5 are visible:

- profile preview latency
- add local proxy cache reuse
- reduce bitmap conversion
- add GPU only where a real bottleneck is measured
- keep drag preview proxy-only

## Known UI Label Notes

Current compact labels may remain in the UI, but engine-facing names must be explicit.

- `Dark` maps to `DarkCircleReduce`.
- `Under Wrinkle` maps to `UnderEyeWrinkleSoften`.
- `Shoulder` maps to `ShoulderNeckBalance`.
- `Distance` inside `Eyes` maps to `EyeDistanceBalance`.
- `Distance` inside `Brows` maps to `BrowDistanceBalance`.

If the UI becomes confusing during visual review, rename `Dark` to `Dark Circle` before engine wiring.

## No-Op Rule

If a required work box, landmark, or mask is unavailable:

```text
return current image unchanged
show Not Ready status
do not write history
do not retry artifact generation from slider ticks
```

This is required for daily-use stability.

