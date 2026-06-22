# BuildBioMask Contract

## 1. Purpose

`BuildBioMask(...)` defines the first stable biological-body mask contract for KRetouchPro.

Its role is:
- isolate biological person structure from clothing and worn accessories
- provide the body-structure parent mask for anatomy-aware analysis and derived masks
- keep biological ownership separate from person-preserve and skin-retouch meanings

This function does not define final background preservation by itself.
It builds the biological ownership layer that later masks depend on.

## 2. Current Stage Definition

At the current project stage:

- `PersonMask` means final visible person preservation
- `SkinMask` means visible skin only
- `BioMask` must remain the biological structure layer between them

Therefore `BuildBioMask(...)` is defined as:

**the mask-construction step that combines biological subregions into one anatomy-owned mask without clothing or accessory ownership**

It is not:
- a skin-only mask builder
- a final person-preserve mask builder
- a clothing detector
- a background compositor

## 3. Function Name

Preferred name:

`BuildBioMask(...)`

Related later functions:

- `BuildPersonMask(...)`
- `BuildSkinMask(...)`
- `BuildHairMask(...)`
- `BuildFaceSkinMask(...)`
- `BuildJawlineMask(...)`
- `BuildDoubleChinMask(...)`

## 4. Ownership

Pillar:

- Mask Construction

This function should construct biological ownership only.
It must not directly:
- replace the background
- classify garments as biological
- smooth skin
- warp geometry
- decide handheld-object preservation

## 5. Primary Use Cases

### 5.1 Biological structure routing

Use `BioMask` to reason about:

- head
- neck
- torso
- upper limbs
- lower limbs

### 5.2 Derived biological submasks

Use `BioMask` as the parent base for:

- `HeadBioMask`
- `NeckBioMask`
- `TorsoBioMask`
- `UpperLimbBioMask`
- `LowerLimbBioMask`

### 5.3 Anatomy-aware protection

Use `BioMask` later to support:

- jawline support
- neck support
- shoulder support
- body-region work-area definition

## 6. Input Contract

The builder should accept the following inputs.

### 6.1 Required

- `HeadBioMask`
- `NeckBioMask`
- `TorsoBioMask`
- `UpperLimbBioMask`
- `LowerLimbBioMask`
- `ImageWidth`
- `ImageHeight`

### 6.2 Recommended

- `FaceRegionMask`
- `HairMask`
- `EarMask`
- `EyebrowMask`
- `EyelashMask`
- `FacialHairMask`
- `NeckRegionMask`
- `ShoulderBioMask`

### 6.3 Optional

- `BuildMode`
  - `Anatomy`
  - `PreserveAssist`
  - `DebugOnly`
- `BioAlphaMode`
- `ConfidenceMap`

## 7. Core Meaning Rule

`BioMask` means biological body ownership only.

It must include:
- face
- visible skin
- scalp hair
- eyebrows
- eyelashes
- ears
- facial hair
- neck
- torso
- arms
- hands
- legs
- feet
- body hair

It must exclude:
- clothing
- shoes
- glasses
- hat
- belt
- watch
- jewelry
- other accessories

## 8. Formula Rule

The formula-spec definition already fixed in the project is:

\[
BioMask = HeadBioMask \cup NeckBioMask \cup TorsoBioMask \cup UpperLimbBioMask \cup LowerLimbBioMask
\]

The current head-side biological definition already includes:

\[
HeadBioMask =
FaceRegionMask
\cup HairMask
\cup EarMask
\cup EyebrowMask
\cup EyelashMask
\cup FacialHairMask
\]

Companion alpha when needed:

\[
BioAlpha = BioMask \cdot SmoothStep(s_b,e_b,d_b)
\]

## 9. Separation Rule

`BioMask` must stay separate from both `PersonMask` and `SkinMask`.

Important separation:

- `BioMask` = biological ownership
- `PersonMask` = biological + clothing + worn-accessory preservation
- `SkinMask` = visible skin only

Do not put clothing into `BioMask`.
Do not shrink `BioMask` down to skin only.

## 10. Output Contract

Preferred result structure:

`BioMaskBuildResult`

Suggested fields:

- `Status`
- `Confidence`
- `BioMask`
- `BioAlpha`
- `HeadBioMask`
- `NeckBioMask`
- `TorsoBioMask`
- `UpperLimbBioMask`
- `LowerLimbBioMask`
- `Warnings`

Minimum required outputs:

- `BioMask`
- optional `BioAlpha`

## 11. Downstream Use Rule

Use `BioMask` for:
- anatomy-aware region ownership
- parent support for derived feature masks
- intermediate input to `BuildPersonMask(...)`

Do not use `BioMask` as:
- final background-replacement preserve mask
- skin-retouch apply mask
- clothing mask

## 12. Debug Outputs

Recommended debug outputs:

- `debug_bio_mask.png`
- `debug_bio_alpha.png`
- `debug_head_bio_mask.png`
- `debug_neck_bio_mask.png`
- `debug_bio_mask_overlay.png`
- `debug_bio_mask_report.json`

Recommended report fields:

- build status
- confidence
- component-region availability
- warnings

## 13. Do-Not-Do Rules

- Do not merge clothing into `BioMask`.
- Do not treat shoes as biological ownership.
- Do not use accessory silhouettes as biological structure.
- Do not silently substitute `PersonMask` for `BioMask`.
- Do not collapse hair and skin into one unnamed region.

## 14. Success Condition

`BuildBioMask(...)` is successful when:

- biological structure is isolated from clothing and accessories
- head, neck, torso, and limbs remain anatomy-owned
- the result can safely feed `BuildPersonMask(...)` and anatomy-derived masks
- the result remains clearly distinct from `SkinMask`

## 15. One-Line Definition

`BuildBioMask(...)` constructs the anatomy-owned biological mask by combining head, neck, torso, and limb biological regions into one clothing-free body-structure layer for downstream person, skin, and feature-mask routing.
