# BuildFaceSkinMask Contract

## 1. Purpose

`BuildFaceSkinMask(...)` defines the first stable face-skin mask contract for KRetouchPro.

Its role is:
- isolate face skin as a narrower target inside broader skin ownership
- provide a safe apply mask for face-focused skin retouch
- keep face skin separate from hair, brows, lashes, and facial hair

This function does not build all visible skin from scratch.
It derives a face-only skin mask from already-defined face and skin evidence.

## 2. Current Stage Definition

At the current project stage:

- `SkinMask` means visible skin in general
- `FaceSkinMask` is the narrower face-only skin target
- face retouch must not spill into scalp hair, eyebrow, eyelash, or beard regions

Therefore `BuildFaceSkinMask(...)` is defined as:

**the mask-construction step that derives a safe face-only skin mask from `FaceRegionMask` and skin evidence**

It is not:
- a full skin mask builder
- a hair mask builder
- a beard mask builder
- a face warp function

## 3. Function Name

Preferred name:

`BuildFaceSkinMask(...)`

Related later functions:

- `BuildSkinMask(...)`
- `BuildSkinSafeMask(...)`
- `BuildJawlineMask(...)`
- `BuildNeckWrinkleMask(...)`
- `ApplySkinRetouch(...)`

## 4. Ownership

Pillar:

- Mask Construction

This function should construct face-skin ownership only.
It must not directly:
- recolor hair
- replace the background
- redefine clothing or accessory ownership
- warp the face geometry

## 5. Primary Use Cases

### 5.1 Face-only skin retouch

Use `FaceSkinMask` for:

- blemish cleanup on facial skin
- face tone correction
- wrinkle / texture work that should stay on face skin

### 5.2 Protect-side routing

Use `FaceSkinMask` to help separate:

- ear correction
- hair color work
- facial hair work

from core face skin.

### 5.3 Derived feature support

Use `FaceSkinMask` as a parent support region for later face-only local masks.

## 6. Input Contract

The builder should accept the following inputs.

### 6.1 Required

- `FaceRegionMask`
- `SkinProb`
- `HairMask`
- `EyebrowMask`
- `EyelashMask`
- `FacialHairMask`
- `ImageWidth`
- `ImageHeight`

### 6.2 Recommended

- `SkinMask`
- `AccessoryMask`
- `FaceBox`
- `LeftEyeMask`
- `RightEyeMask`
- `LipMask`

### 6.3 Optional

- `BuildMode`
  - `SkinRetouch`
  - `FeatureSupport`
  - `DebugOnly`
- `FaceSkinConfidence`
- `ExclusionPolicy`

## 7. Core Meaning Rule

`FaceSkinMask` means visible face skin only.

It should include:
- forehead skin
- cheek skin
- nose skin
- philtrum skin
- chin skin
- jaw-front facial skin when it is skin

It should exclude:
- scalp hair
- eyebrow
- eyelash
- facial hair
- ear skin
- lips
- clothing
- background

## 8. Formula Rule

The formula-spec definition already fixed in the project is:

\[
FaceSkinMask =
FaceRegionMask
\cdot SkinProb
\cdot (1 - HairMask)
\cdot (1 - EyebrowMask)
\cdot (1 - EyelashMask)
\cdot (1 - FacialHairMask)
\]

This keeps `FaceSkinMask` narrower than `FaceRegionMask`.

## 9. Separation Rule

`FaceSkinMask` must stay separate from:

- `HairMask`
- `EyebrowMask`
- `EyelashMask`
- `FacialHairMask`
- `EarMask`
- `LipMask`

Important meaning split:

- `SkinMask` = broader visible skin ownership
- `FaceSkinMask` = face-only skin target
- `FaceRegionMask` = face carrier region, not automatically skin-only

Do not treat `FaceRegionMask` itself as `FaceSkinMask`.

## 10. Output Contract

Preferred result structure:

`FaceSkinMaskBuildResult`

Suggested fields:

- `Status`
- `Confidence`
- `FaceSkinMask`
- `Warnings`

Minimum required output:

- `FaceSkinMask`

## 11. Downstream Use Rule

Use `FaceSkinMask` for:
- face-only retouch apply mask
- protect/block routing for nearby non-skin face features
- narrower face feature derivation

Do not use `FaceSkinMask` as:
- full `SkinMask`
- `FaceRegionMask`
- jawline boundary by itself
- ear mask

## 12. Debug Outputs

Recommended debug outputs:

- `debug_face_skin_mask.png`
- `debug_face_skin_mask_overlay.png`
- `debug_face_skin_mask_report.json`

Recommended report fields:

- build status
- confidence
- source face region used
- exclusion masks used
- warnings

## 13. Do-Not-Do Rules

- Do not let eyebrow become face skin.
- Do not let eyelash become face skin.
- Do not let beard or mustache become face skin.
- Do not silently widen `FaceSkinMask` into ear or neck skin.
- Do not use face carrier geometry alone as proof of skin ownership.

## 14. Success Condition

`BuildFaceSkinMask(...)` is successful when:

- face-only skin retouch can run without leaking into nearby hair regions
- facial hair remains separate
- `FaceSkinMask` stays narrower and safer than `FaceRegionMask`

## 15. One-Line Definition

`BuildFaceSkinMask(...)` constructs the safe face-only skin mask from `FaceRegionMask` and skin evidence so that facial retouch stays on actual face skin and avoids hair, brows, lashes, and facial hair.
