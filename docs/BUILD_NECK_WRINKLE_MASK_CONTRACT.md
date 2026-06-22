# BuildNeckWrinkleMask Contract

## 1. Purpose

`BuildNeckWrinkleMask(...)` defines the first stable neck-wrinkle mask contract for KRetouchPro.

Its role is:
- isolate wrinkle or crease features inside the neck region
- support neck-wrinkle retouch without collapsing the whole neck into one target
- keep neck-wrinkle texture meaning separate from jawline and double-chin shape meaning

This function is not a full neck-skin mask builder.
It derives a texture/status feature mask inside valid neck support.

## 2. Current Stage Definition

At the current project stage:

- neck skin and lower-face support are active priorities
- neck wrinkles are texture/status features, not whole-region ownership
- neck-wrinkle work must remain separate from jawline and double-chin work

Therefore `BuildNeckWrinkleMask(...)` is defined as:

**the mask-construction step that derives wrinkle or crease features inside valid neck skin support**

It is not:
- a neck-skin mask builder
- a jawline boundary builder
- a double-chin mask builder
- a clothing-boundary detector

## 3. Function Name

Preferred name:

`BuildNeckWrinkleMask(...)`

Related later functions:

- `BuildSkinMask(...)`
- `BuildJawlineMask(...)`
- `BuildDoubleChinMask(...)`
- `ApplySkinRetouch(...)`

## 4. Ownership

Pillar:

- Mask Construction

This function should construct a texture/status feature mask only.
It must not directly:
- widen into the full neck skin region
- redefine jawline geometry
- include clothing folds
- replace the background

## 5. Primary Use Cases

### 5.1 Neck-wrinkle softening

Use `NeckWrinkleMask` for:

- neck crease softening
- neck line cleanup
- local neck texture retouch

### 5.2 Strength control

Use `NeckWrinkleMask` to vary intensity per visible wrinkle group rather than retouching the whole neck evenly.

### 5.3 Lower-face feature separation

Use `NeckWrinkleMask` to keep:

- jawline boundary
- double-chin region
- general neck skin

from collapsing into one lower-face mask.

## 6. Input Contract

The builder should accept the following inputs.

### 6.1 Required

- `NeckSkinMask`
- `NeckROI`
- `WrinkleProb`
- `ImageWidth`
- `ImageHeight`

### 6.2 Recommended

- `SkinMask`
- `JawlineMask`
- `DoubleChinMask`
- `ClothingBoundaryDetectionResult`
- `NeckBaseLine`

### 6.3 Optional

- `BuildMode`
  - `Retouch`
  - `DebugOnly`
- `LineSensitivity`
- `DirectionPolicy`
  - `Horizontal`
  - `Curved`
  - `Mixed`

## 7. Core Meaning Rule

`NeckWrinkleMask` means wrinkle or crease features visible on neck skin.

It should include:
- horizontal neck lines
- curved neck fold lines
- oblique neck creases when visibly inside neck skin

It should exclude:
- clothing collar
- necklace
- jawline boundary
- full double-chin shape region
- full neck skin region
- background

## 8. Formula Rule

The formula-spec document already places `NeckWrinkleMask` in the wrinkle-feature family:

\[
WrinkleMask = SkinMask \cdot WrinkleProb
\]

For the neck-specific contract, the safe narrowed form is:

\[
NeckWrinkleMask =
NeckSkinMask
\cdot WrinkleProb
\cdot (1 - ClothingMask)
\cdot (1 - AccessoryMask)
\]

If `ClothingMask` or `AccessoryMask` are unavailable, the builder should still stay bounded inside valid `NeckSkinMask` and return warning output when needed.

## 9. Separation Rule

`NeckWrinkleMask` must stay separate from:

- `JawlineMask`
- `DoubleChinMask`
- `ClothingMask`
- `AccessoryMask`
- full `NeckSkinMask`

Important meaning split:

- `NeckWrinkleMask` = local wrinkle/crease feature
- `JawlineMask` = boundary/reference structure
- `DoubleChinMask` = under-jaw shape feature

Do not use `NeckWrinkleMask` as a substitute for whole-neck retouch.

## 10. Output Contract

Preferred result structure:

`NeckWrinkleMaskBuildResult`

Suggested fields:

- `Status`
- `Confidence`
- `NeckWrinkleMask`
- `Warnings`

Minimum required output:

- `NeckWrinkleMask`

## 11. Downstream Use Rule

Use `NeckWrinkleMask` for:
- neck-crease retouch
- local wrinkle intensity control
- texture/status routing inside neck work

Do not use `NeckWrinkleMask` as:
- full `NeckSkinMask`
- `JawlineMask`
- `DoubleChinMask`
- collar or necklace mask

## 12. Debug Outputs

Recommended debug outputs:

- `debug_neck_wrinkle_mask.png`
- `debug_neck_wrinkle_mask_overlay.png`
- `debug_neck_wrinkle_mask_report.json`

Recommended report fields:

- build status
- confidence
- wrinkle direction policy
- neck support region used
- warnings

## 13. Do-Not-Do Rules

- Do not let collar edges become neck wrinkles.
- Do not let necklace edges become neck wrinkles.
- Do not widen wrinkle retouch to the entire neck by default.
- Do not merge jawline or double-chin structure into wrinkle-only logic.

## 14. Success Condition

`BuildNeckWrinkleMask(...)` is successful when:

- visible neck creases can be targeted without affecting the whole neck
- collar and accessory edges stay out
- wrinkle logic remains separate from jawline and double-chin shape logic

## 15. One-Line Definition

`BuildNeckWrinkleMask(...)` constructs the neck-wrinkle feature mask from valid neck skin support and wrinkle evidence so that neck-crease retouch stays local and does not collapse into jawline, clothing, or full-neck edits.
