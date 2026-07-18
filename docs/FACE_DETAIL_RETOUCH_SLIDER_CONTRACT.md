# Face Detail Retouch Slider Contract

## Purpose

This document defines the connection contract for the `Face Detail` retouch panel.
It converts the UI sliders into engine-facing operation names, target scopes, mask or landmark-region labels, preview policy, and implementation order.

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

## Mask Terminology Note

Some early rows use `Mask`, `ApplyMask`, `ProtectMask`, or named mask IDs because the exact MediaPipe 478-point face landmark index map was not known when the draft was written.
In this document, those mask names are directional target-area placeholders unless a later implementation note explicitly marks them as detector-backed masks.

Practical implementation must bind each slider to exact landmark indices, landmark groups, or a verified detector mask before processing.
When exact landmark indices are available, use those numbers as the concrete implementation target and keep the mask name only as a human-readable region label.

## Current Code Alignment

The current application implementation is a rough landmark-driven route, not the full detector-mask request pipeline.
It uses `FaceDetailAdjustmentSnapshot` values from `FaceDetailTabView`, builds local warp plans from cached MediaPipe landmarks, and applies a small number of tone passes.

Current UI events pass compact UI tag names such as `NoseSize`, `LeftUnderEye`, and `DoubleChin`.
The `OperationId` names in the inventory tables remain canonical engine-facing names for the future typed request model.

Rule status in the inventory tables means the design rule is defined.
It does not mean a detector-backed mask implementation is complete.
For the current code state, use the landmark binding table below as the implementation source of truth.

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

- cached landmarks and exact landmark groups
- documented detector targets
- documented mask labels or verified detector masks
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

Current Face Detail sliders use two amount families.

Bidirectional structure sliders use a centered value:

```text
0   = maximum approved negative direction
50  = neutral / no change
100 = maximum approved positive direction
```

Current centered implementation:

```text
CenteredAmount = (SliderValue - 50) / 50
```

One-way cleanup sliders use an unsigned value:

```text
0   = no cleanup / no change
100 = maximum approved cleanup
```

Current one-way cleanup sliders:

- `LeftDarkCircle`
- `RightDarkCircle`
- `LeftUnderEye`
- `RightUnderEye`
- `NeckWrinkle`
- `DoubleChin`

Each operation maps its normalized amount to its own conservative maximum.
Preview and commit must use the same mapping.

## Preview And Commit Rule

Face Detail sliders are high-use retouch controls.
They must follow the daily-use stability standard.

Preview:

- proxy only
- latest result wins
- no history write
- no uncached AI/Python/GPU work per slider tick
- local work area or landmark region only

Commit:

- serialized per photo
- full-resolution allowed
- version checked before result assignment
- one history entry per committed operation group

## Request Shape

Use one shared request shape for all Face Detail sliders in the future typed engine route.
The current code path does not yet create this request object; it passes a UI operation tag and the full `FaceDetailAdjustmentSnapshot`.

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
| Tilt | `EyeCornerTilt` | pair warp | left/right outer eye ROI | `LeftEyeMask`, `RightEyeMask` | ready |
| Dark | `DarkCircleReduce` | pair tone | left/right under-eye ROI | `DarkCircleMask` | ready |
| Under Wrinkle | `UnderEyeWrinkleSoften` | pair texture | left/right under-eye ROI | `UndereyeMask` refined by wrinkle evidence | needs new mask builder or fallback |

Current implementation note:

- `Dark` and `Under Wrinkle` both use the under-eye landmark region and a tone/soften pass.
- `Under Wrinkle` is not yet a wrinkle-evidence texture mask.

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
LeftEyeBundle  = left inner corner + left outer corner + eyelid anchors
RightEyeBundle = right inner corner + right outer corner + eyelid anchors
Delta = CenteredAmount * approved horizontal bundle shift
```

`Distance` is not a pupil-center spacing control.
Treat each eye as one eye bundle from inner corner to outer corner, including the visible eyelid structure.
Move the left and right eye bundles symmetrically around the facial midline.
The visible order target is the gap between the two inner eye corners.
When narrowing, the left inner corner and right inner corner must move closer together.
When widening, those same inner corners must move farther apart.
The current rough implementation shifts the eye contour landmarks together and adds inner-corner and outer-corner support points so the whole eye bundle follows the spacing change.

The operation must not stretch the eye shape just to change spacing.
The surrounding skin follows with a local falloff, but the nose, brows, and full face must remain damped.
`50` is neutral.
Values above `50` widen the space between the eye bundles.
Values below `50` narrow the space between the eye bundles.

#### `EyeHeightOpen`

Rule:

```text
UpperEyelid contour opens upward by 80% of the height effect
LowerEyelid contour opens downward by 20% of the height effect
Eye corners remain damped and follow the eyelid curve softly
```

This is an eye-opening control, not a whole-eye vertical move.
The center eyelid region above the iris should carry the strongest visual change.
The visual budget is roughly 70% eyelid-height opening and 30% visible iris reveal.
Do not scale the pupil or iris as a biological object.
Keep the visible pupil and iris circular whenever possible.
Avoid vertical-only oval deformation in the eye center.
The intended result is that the eyelids reveal more of the already-existing iris area.
Use a small horizontal companion expansion so the opening does not read as a vertical pupil stretch.
Use eyelid curve targets from `DETECTOR_TARGET_AND_MASK_IDS.md`.
Protect brows and lashes.

#### `EyeCornerTilt`

Reference:

- `CORE_FORMULA_COMPANION.md` formula 37

Rule:

```text
Inner corner cluster = strong tilt anchor
Outer corner cluster = strong tilt anchor
Corner support points follow the same tilt direction
Eye center remains protected from oval pupil/iris distortion
```

Current rough implementation caps `100` at 30% of the previous tilt strength.
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

This follows the same spacing concept as `EyeDistanceBalance`.
It is not a brow thickness or brow shape control.
Move the left and right brow bundles symmetrically so the visible gap between the inner brow heads changes.
When narrowing, the two inner brow heads move closer together.
When widening, the two inner brow heads move farther apart.
The current rough implementation shifts each brow landmark group and adds an inner-brow support point.
Current max displacement is intentionally half of the first rough connection strength so the slider does not read as fully applied at mid travel.

#### `BrowThicknessAdjust`

This is a thickness / volume control, not a brow-position control.
Use a curve-normal expansion around the eyebrow body, similar in intent to `EyeHeightOpen`.
The brow centerline stays comparatively stable while upper and lower brow evidence spreads apart.

```text
UpperBrowEvidence moves upward by the approved thickness amount
LowerBrowEvidence moves downward by the approved thickness amount
Brow centerline remains protected from whole-brow translation
```

Do not paint outside the brow work box.
The current rough implementation uses brow landmark vertical spread around the local brow center.

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

Current implementation note:

- Nose sliders currently use landmark indices `4`, `168`, `98`, and `327`.
- Additional tapered row controls and side guard points are derived synthetically from those landmarks.
- `Size` changes the horizontal width of the full visible nose mass from the bridge root to the nostril base. It is not a nose-tip-only scale.
- `Length` extends the full nose mass along the bridge axis while keeping the bridge root fixed.
- The current rough `Length` route does not push the philtrum or mouth. The earlier follower path was removed because it moved the upper-lip area too strongly.
- Upper nose guards keep the inner-eye area from following the nose-width warp.
- `Bridge` is currently a warp control, not a separate bridge tone mask.

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

Use a centered horizontal width adjustment for the whole tapered nose mass.
`50` is neutral, lower values compress the nose toward its center axis, and higher values widen it.
The bridge root receives the lowest movement, the middle side planes follow progressively, and the nostril base receives the full approved width movement.
Do not flatten the bridge, drag the inner eyes, or move the mouth.

#### `NoseLengthRefine`

Move the nose mass progressively along the bridge axis, from a fixed bridge root to the strongest movement at the tip.
Nostril support follows at a lower weight so the lower nose remains connected.
Keep the bridge root fixed and do not push the philtrum or mouth in the current rough route.

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
| Width | `MouthWidthRefine` | warp | mouth span | `LipMask` | ready |
| Upper Lip | `UpperLipThickness` | warp | upper lip | `UpperLipMask` | ready |
| Lower Lip | `LowerLipThickness` | warp | lower lip | `LowerLipMask` | ready |
| Corner | `MouthCornerLift` | pair warp | mouth corners | left/right corner ROI | ready |

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

#### `MouthWidthRefine`

Use horizontal-only mouth span refinement.
Keep vertical lip thickness damped.
The former integrated size slider is intentionally removed because width and lip controls define the mouth dimensions more clearly.

#### `UpperLipThickness` / `LowerLipThickness`

Reference:

- `CORE_FORMULA_COMPANION.md` formula 41

Use upper/lower masks split by lip midline.
Do not move teeth or inner mouth.
Keep mouth corners stable and prevent the inner lip seam from jumping.
For `Upper Lip`, keep the full lower/inner upper-lip line anchored, not only the center point.
Increase only the upper outer lip vertically with an arc falloff: center strongest, ends near zero.
For `Lower Lip`, keep the full upper/inner lower-lip line anchored and increase only the lower outer lip downward with the same arc falloff.
The lower outer arc may include the outer lower-lip side points so the curve broadens smoothly instead of peaking only at the center.
These sliders own the vertical lip-volume adjustment; the former vertical slider is intentionally removed.
Current rough implementation applies 750% of the first arc-falloff lip strength for visibility tuning.
The central inner seam uses four stronger anchor points per lip to prevent center-line jumping.
Lip anchors are active for lip-thickness sliders only; `Mouth Corner` uses its own local corner ROI and body anchors.

#### `MouthCornerLift`

Reference:

- `CORE_FORMULA_COMPANION.md` formula 42

Use local corner lift.
A linked pair applies the same lift to both corners.
An unlinked pair can correct asymmetric corners.
The former `SmileBalance` slider is intentionally folded into this control.
Pull the mouth corner diagonally upward and outward.
Use a 70% upward and 30% outward vector.
Keep this as a local corner liquify pull in separate left/right corner warp plans.
The mouth corner is strongest, the upper/lower corner side points follow lightly, and synthetic midpoint brush controls spread the movement into a small corner-skin area.
Do not directly group broad cheek or nasolabial landmarks into the same pull; that reads as a block movement.
Do not run this through the same wide mouth-span warp plan as `Mouth Width`, because that makes the whole mouth move as one block.
Use inner lip body anchors so the mouth slit and central lip seam stay stable while the corner pocket lifts.

Current rough implementation note:

- Main destination points are `61`, `185`, `146` on the left and `291`, `409`, `375` on the right.
- The inner seam guard is strongest at `78`, `95`, `308`, and `324`, with additional upper/lower line and segment anchors.
- Gaussian interpolation and the strong seam anchors reduce visible corner travel, so the implementation uses tuned horizontal/vertical coefficients plus a final `1.30` strength multiplier.
- The 70% upward / 30% outward rule remains the visible motion target, not a literal coefficient split before interpolation.

### Neck

| UI label | OperationId | Type | Target | ApplyMask | Rule status |
| --- | --- | --- | --- | --- | --- |
| Slim | `NeckSlimRefine` | warp | `neck_work_box` | `NeckMask` / `NeckBioMask` | ready with conservative limits |
| Length | `NeckLengthRefine` | warp | neck axis ROI | `NeckMask` / `NeckBioMask` | ready with conservative limits |
| Wrinkle | `NeckWrinkleSoften` | texture | neck wrinkle bands | `NeckWrinkleMask` | ready |
| Double Chin | `DoubleChinReduce` | tone/warp | under jaw | `DoubleChinWorkMask` | ready |
| Side | `SideNeckBalance` | pair warp | left/right side neck | side neck ROI | ready |
| Trapezius | `TrapeziusLowering` | pair warp | trapezius / shoulder-neck hump | `ShoulderHumpMask` / `TrapeziusMask` | ready |

Current implementation note:

- Neck, Double Chin, and Trapezius sliders currently use chin/jaw landmarks `152`, `172`, and `397` plus synthetic support points.
- `Double Chin` combines landmark warp with a simple under-jaw tone ellipse.
- `Neck Wrinkle` now uses a neck tone/texture softening pass and does not contribute to lower-face warp.
- The final `NeckWrinkleMask` detector is still required for production-grade wrinkle-band targeting.

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
The current rough implementation uses a chin/jaw-derived neck ROI with local texture smoothing until the verified mask is available.

#### `DoubleChinReduce`

Reference:

- `BUILD_DOUBLE_CHIN_MASK_CONTRACT.md`

Use `DoubleChinWorkMask`.
Prefer tone/shadow cleanup first, then conservative shape support.

#### `SideNeckBalance`

Pair operation for side-neck asymmetry.
Use left/right side neck ROIs and protect clothing/hair.

#### `TrapeziusLowering`

Use `ShoulderHumpMask`, `TrapeziusMask`, or shoulder-neck work area.
Lower the visible trapezius hump without treating it as the shoulder joint.
Do not confuse trapezius lowering with clothing shoulder seams.

## Current Landmark Binding Table

This table records the current rough implementation target.
It should be updated whenever the code changes landmark indices or replaces a landmark region with a verified detector mask.

| UI label | Current UI tag / snapshot field | Canonical operation | Amount family | Current landmark binding |
| --- | --- | --- | --- | --- |
| Eye Size | `EyeSize` | `EyeOverallScale` | centered `50` neutral | left eye `33, 133, 159, 145`; right eye `263, 362, 386, 374` |
| Eye Distance | `EyeDistance` | `EyeDistanceBalance` | centered `50` neutral | eye contour bundle shift; left `33, 7, 163, 144, 145, 153, 154, 155, 133, 173, 157, 158, 159, 160, 161, 246`; right `362, 382, 381, 380, 374, 373, 390, 249, 263, 466, 388, 387, 386, 385, 384, 398`; no pupil-center target |
| Eye Height | `LeftEyeHeight`, `RightEyeHeight` | `EyeHeightOpen` | centered `50` neutral | eyelid opening; upper contour 80%, lower contour 20%, small horizontal companion, iris guard anchors to keep pupil/iris round |
| Eye Tilt | `LeftEyeTilt`, `RightEyeTilt` | `EyeCornerTilt` | centered `50` neutral | inner/outer eye-corner clusters plus corner support points |
| Dark | `LeftDarkCircle`, `RightDarkCircle` | `DarkCircleReduce` | one-way `0` none | under-eye tone region from `33, 133, 145` and `263, 362, 374` |
| Under Wrinkle | `LeftUnderEye`, `RightUnderEye` | `UnderEyeWrinkleSoften` | one-way `0` none | current shared under-eye tone/soften region; no wrinkle mask yet |
| Brow Distance | `BrowDistance` | `BrowDistanceBalance` | centered `50` neutral | brow bundle spacing; left brow `70, 63, 105, 66, 107`; right brow `336, 296, 334, 293, 300`; inner brow support point |
| Brow Thickness | `LeftBrowThickness`, `RightBrowThickness` | `BrowThicknessAdjust` | centered `50` neutral | rough vertical spread around brow centerline; not whole-brow position |
| Brow Tilt | `LeftBrowTilt`, `RightBrowTilt` | `BrowTiltAdjust` | centered `50` neutral | same brow landmark groups |
| Brow Arch | `LeftBrowArch`, `RightBrowArch` | `BrowArchAdjust` | centered `50` neutral | same brow landmark groups |
| Brow Position | `LeftBrowPosition`, `RightBrowPosition` | `BrowPositionLift` | centered `50` neutral | same brow landmark groups |
| Brow Tail | `LeftBrowTail`, `RightBrowTail` | `BrowTailAdjust` | centered `50` neutral | same brow landmark groups |
| Nose Size | `NoseSize` | `NoseSizeRefine` | centered `50` neutral | nose tip `4`, bridge `168`, left nostril `98`, right nostril `327` |
| Nose Length | `NoseLength` | `NoseLengthRefine` | centered `50` neutral | same nose landmark group |
| Nose Bridge | `NoseBridge` | `NoseBridgeRefine` | centered `50` neutral | same nose group plus synthetic side controls |
| Nose Width | `NoseWidth` | `NoseWidthRefine` | centered `50` neutral | same nose group plus synthetic side controls |
| Nose Tip | `NoseTip` | `NoseTipRefine` | centered `50` neutral | same nose landmark group |
| Nostril | `LeftNostril`, `RightNostril` | `NostrilBalance` | centered `50` neutral | left nostril `98`, right nostril `327` |
| Mouth Width | `MouthWidth` | `MouthWidthRefine` | centered `50` neutral | mouth corner clusters plus corners `61`, `291` |
| Upper Lip | `UpperLip` | `UpperLipThickness` | centered `50` neutral | move upper outer lip center arc `40, 39, 37, 0, 267, 269, 270`; anchor full lower/inner upper-lip line and lip ends `78, 191, 80, 81, 82, 13, 312, 311, 310, 415, 308, 185, 409`; strong center anchors `82, 13, 312, 311` |
| Lower Lip | `LowerLip` | `LowerLipThickness` | centered `50` neutral | move lower outer lip broad arc `146, 91, 181, 84, 17, 314, 405, 321, 375`; anchor full upper/inner lower-lip line `78, 95, 88, 178, 87, 14, 317, 402, 318, 324, 308`; strong center anchors `87, 14, 317, 402` |
| Mouth Corner | `LeftMouthCorner`, `RightMouthCorner` | `MouthCornerLift` | centered `50` neutral | separate left/right local corner warp plans; 70% up, 30% outward; core `61 / 291` 100%; upper corner `185 / 409`; lower corner `146 / 375`; very light side followers `57,76,186 / 287,306,410`; midpoint brush controls spread motion locally; inner lip body anchors hold the mouth slit; broad cheek landmarks are not directly grouped |
| Neck Slim | `NeckSlim` | `NeckSlimRefine` | centered `50` neutral | chin `152`, left jaw `172`, right jaw `397` |
| Neck Length | `NeckLength` | `NeckLengthRefine` | centered `50` neutral | same chin/jaw landmark group |
| Neck Wrinkle | `NeckWrinkle` | `NeckWrinkleSoften` | one-way `0` none | chin/jaw-derived neck ROI tone/texture softening; no shape warp |
| Double Chin | `DoubleChin` | `DoubleChinReduce` | one-way `0` none | chin `152`, left jaw `172`, right jaw `397`; under-jaw tone ellipse |
| Side Neck | `LeftSideNeck`, `RightSideNeck` | `SideNeckBalance` | centered `50` neutral | chin/jaw group with side-neck direction weights |
| Trapezius | `LeftTrapezius`, `RightTrapezius` | `TrapeziusLowering` | centered `50` neutral | chin/jaw group plus synthetic trapezius-lowering support points |

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

Current code status:

- `FaceDetailTabView` change events are connected.
- preview version token and commit serialization gate are connected.
- typed `FaceDetailOperationId` and `FaceDetailRetouchRequest` are not yet implemented.

### Phase 2 - Mask And Work Area Resolution

Implement or reuse:

- eye work boxes
- brow work boxes
- nose work box
- mouth work box
- neck work box
- exact landmark group lookup for 478-point face landmarks
- optional mask lookup by `MaskId` when a verified detector mask exists
- fallback no-op when required landmarks or masks are missing

### Phase 3 - Low-Risk Texture/Tone Operations

Connect first:

- `DarkCircleReduce`
- `UnderEyeWrinkleSoften`
- `NeckWrinkleSoften`
- `DoubleChinReduce` tone pass

These should not warp facial geometry.

Current code status:

- `DarkCircleReduce` and `UnderEyeWrinkleSoften` are approximated by one under-eye tone/soften pass.
- `DoubleChinReduce` has a tone pass plus a conservative lower-face warp.
- `NeckWrinkleSoften` is a rough neck texture/tone pass; it still needs the verified `NeckWrinkleMask`.

### Phase 4 - Landmark-Based Local Warp Operations

Connect next:

- `EyeOverallScale`
- `EyeHeightOpen`
- `EyeCornerTilt`
- `UpperLipThickness`
- `LowerLipThickness`
- `MouthCornerLift`
- `NostrilBalance`

Current code status:

- the listed operations are connected through rough landmark warps.
- tuning remains required per slider.

### Phase 5 - Conservative Shape Refinement

Connect after visual confirmation:

- brow operations
- nose size/length/bridge/width/tip
- mouth width
- neck slim/length/side/trapezius

Current code status:

- these controls are also roughly connected through landmark warps.
- the current goal is visible response first, then a second tuning pass.

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
- `Trapezius` maps to `TrapeziusLowering`.
- `Distance` inside `Eyes` maps to `EyeDistanceBalance`.
- `Distance` inside `Brows` maps to `BrowDistanceBalance`.

If the UI becomes confusing during visual review, rename `Dark` to `Dark Circle` before engine wiring.

## No-Op Rule

If a required work box, landmark group, or verified mask is unavailable:

```text
return current image unchanged
show Not Ready status
do not write history
do not retry artifact generation from slider ticks
```

This is required for daily-use stability.

