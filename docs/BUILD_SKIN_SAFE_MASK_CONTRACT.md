# BuildSkinSafeMask Contract

## 1. Purpose

`BuildSkinSafeMask(...)` defines the first stable skin-retouch safety-mask contract for KRetouchPro.

Its role is:
- narrow `SkinMask` into a safer retouch target
- exclude nearby non-skin structures that are commonly damaged by skin smoothing or tone work
- provide the final apply mask for skin-only retouch routing

This function does not detect new skin from scratch.
It refines an existing skin result into a safer operational mask.

## 2. Current Stage Definition

At the current project stage:

- `SkinMask` means visible skin ownership
- skin retouch still needs a stricter execution mask than broad visible skin
- hair, brows, lashes, facial hair, clothing, and accessories must stay out
- eye, nose, and jawline structure also need protection from broad skin smoothing

Therefore `BuildSkinSafeMask(...)` is defined as:

**the mask-construction step that turns `SkinMask` into a safer skin-retouch work mask by subtracting non-skin structures**

It is not:
- a new skin detector
- a full person mask builder
- a texture filter by itself
- a clothing or accessory classifier

## 3. Function Name

Preferred name:

`BuildSkinSafeMask(...)`

Related later functions:

- `BuildSkinMask(...)`
- `BuildFaceSkinMask(...)`
- `ApplySkinRetouch(...)`
- `CorrectAcneSpots(...)`
- `SoftenWrinkles(...)`

## 4. Ownership

Pillar:

- Mask Construction

This function should construct an operational safety mask only.
It must not directly:
- retouch pixels
- recolor hair
- replace the background
- redefine person or clothing ownership

## 5. Primary Use Cases

### 5.1 Skin retouch apply mask

Use `SkinSafeMask` as the final safe apply mask for:

- skin smoothing
- skin tone balancing
- blemish cleanup
- wrinkle softening on valid skin

### 5.2 Protect/block routing

Use `SkinSafeMask` to prevent leakage into:

- eye
- scalp hair
- eyebrow
- eyelash
- nose structure
- jawline boundary
- facial hair
- clothing
- accessories

### 5.3 Safer derived skin work

Use `SkinSafeMask` as the operation-ready version of `SkinMask` when a tool should act on skin but not on nearby non-skin edges.

## 6. Input Contract

The builder should accept the following inputs.

### 6.1 Required

- `SkinMask`
- `EyeMask`
- `HairMask`
- `EyebrowMask`
- `EyelashMask`
- `NoseMask`
- `JawlineMask`
- `FacialHairMask`
- `ClothingMask`
- `AccessoryMask`
- `ImageWidth`
- `ImageHeight`

### 6.2 Recommended

- `FaceSkinMask`
- `NeckSkinMask`
- `UnderJawSkinMask`
- `LipMask`
- `EarMask`

### 6.3 Optional

- `BuildMode`
  - `SkinRetouch`
  - `DebugOnly`
- `EdgeProtectionRadius`
- `SafeMaskPolicy`

## 7. Core Meaning Rule

`SkinSafeMask` means the safe execution mask for skin-only retouch.

It should include:
- valid facial skin
- valid neck skin
- valid exposed skin that survives non-skin exclusion

It should exclude:
- eye
- scalp hair
- eyebrow
- eyelash
- nose structure
- jawline boundary
- facial hair
- clothing
- accessories
- background

## 8. Formula Rule

The formula-spec definition already fixed in the project is:

\[
SkinSafeMask =
SkinMask
\cdot (1 - EyeMask)
\cdot (1 - HairMask)
\cdot (1 - EyebrowMask)
\cdot (1 - EyelashMask)
\cdot (1 - NoseMask)
\cdot (1 - JawlineMask)
\cdot (1 - FacialHairMask)
\cdot (1 - ClothingMask)
\cdot (1 - AccessoryMask)
\]

Operational routing form:

\[
SkinRetouchWorkMask = SkinSafeMask
\]

## 9. Separation Rule

`SkinSafeMask` must stay separate from:

- `PersonMask`
- `HairMask`
- `ClothingMask`
- `AccessoryMask`
- `BackgroundMask`

Important meaning split:

- `SkinMask` = visible skin ownership
- `SkinSafeMask` = operationally safe skin-retouch target

Do not silently treat `SkinMask` and `SkinSafeMask` as always identical.

## 10. Output Contract

Preferred result structure:

`SkinSafeMaskBuildResult`

Suggested fields:

- `Status`
- `Confidence`
- `SkinSafeMask`
- `Warnings`

Minimum required output:

- `SkinSafeMask`

## 11. Downstream Use Rule

Use `SkinSafeMask` for:
- skin-only retouch apply routing
- acne / wrinkle / tone tools that should stay on skin
- final safe-mask execution in preview and export

Do not use `SkinSafeMask` as:
- a broad person mask
- a hair exclusion source for all unrelated tools
- a substitute for `SkinMask` in ownership definitions

## 12. Debug Outputs

Recommended debug outputs:

- `debug_skin_safe_mask.png`
- `debug_skin_safe_mask_overlay.png`
- `debug_skin_safe_mask_report.json`

Recommended report fields:

- build status
- confidence
- exclusion masks used
- safe-mask shrink amount
- warnings

## 13. Do-Not-Do Rules

- Do not let hair remain inside the final retouch mask.
- Do not let eye, nose, or jawline structure remain unprotected inside the final retouch mask.
- Do not let eyebrow or eyelash remain inside the final retouch mask.
- Do not let beard remain inside the final retouch mask.
- Do not allow clothing or accessory bleed into the final skin retouch target.
- Do not widen `SkinSafeMask` back into generic `PersonMask`.

## 14. Success Condition

`BuildSkinSafeMask(...)` is successful when:

- skin-retouch tools can run without leaking into nearby non-skin regions
- the result remains close enough to valid visible skin
- the operational mask is safer than raw `SkinMask`

## 15. One-Line Definition

`BuildSkinSafeMask(...)` constructs the safe operational skin-retouch mask by subtracting eye, hair, brow, lash, nose, jawline, facial-hair, clothing, and accessory protection regions from `SkinMask`, so that skin-only tools stay on valid skin without flattening key facial structure.
