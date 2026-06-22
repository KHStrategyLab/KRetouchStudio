# BuildHairMask Contract

## 1. Purpose

`BuildHairMask(...)` defines the first stable scalp-hair mask contract for KRetouchPro.

Its role is:
- isolate visible scalp-hair ownership around the head
- provide the preserve-side hair region for background replacement and edge-safe compositing
- provide the base region for hair-only color and local hair tools

This function is not the general body-hair detector.
It builds the head-hair region used in portrait work.

## 2. Current Stage Definition

At the current project stage:

- `HairMask` must stay separate from `SkinMask`
- `HairMask` must stay separate from `EyebrowMask`, `EyelashMask`, and `FacialHairMask`
- hair-edge preservation is critical for background replacement and preview quality

Therefore `BuildHairMask(...)` is defined as:

**the mask-construction step that turns head-side hair candidates into a stable scalp-hair preserve mask**

It is not:
- an eyebrow mask builder
- an eyelash mask builder
- a facial-hair builder
- a skin-retouch mask builder

## 3. Function Name

Preferred name:

`BuildHairMask(...)`

Related later functions:

- `BuildHairColorTargetMask(...)`
- `BuildPersonMask(...)`
- `BuildBioMask(...)`
- `BuildSubjectMatte(...)`
- `DetectHairRegion(...)`
- `DetectHairlineRegion(...)`

## 4. Ownership

Pillar:

- Mask Construction

This function should construct head-hair ownership only.
It must not directly:
- recolor the hair
- replace the background
- redefine face skin
- redefine clothing ownership
- merge eyebrow or beard regions into scalp hair

## 5. Primary Use Cases

### 5.1 Background replacement edge preservation

Use `HairMask` and `HairAlpha` to preserve hair silhouette and soft edge detail.

### 5.2 Hair-only retouch routing

Use `HairMask` as the apply-side parent for:

- hair color
- hair tone
- hair detail protection

### 5.3 Head-side ownership support

Use `HairMask` as part of:

- `HeadBioMask`
- `PersonMask`
- `PersonAlpha`

## 6. Input Contract

The builder should accept the following inputs.

### 6.1 Required

- `HairCandidateMask`
- `HeadROI`
- `ImageWidth`
- `ImageHeight`

### 6.2 Recommended

- `BioMask`
- `FaceRegionMask`
- `SkinProb`
- `HairTextureProb`
- `AccessoryMask`
- `HairlineGuide`

### 6.3 Optional

- `BuildMode`
  - `Preserve`
  - `HairColor`
  - `DebugOnly`
- `HairAlphaMode`
- `EdgeFeatherRadius`

## 7. Core Meaning Rule

`HairMask` means visible scalp-hair ownership around the head.

It should include:
- front hair
- side hair
- back hair
- crown hair
- baby hair when visually connected to scalp hair
- head-connected loose hair that still belongs to scalp hair silhouette

It should exclude:
- face skin
- ear skin
- eyebrows
- eyelashes
- facial hair
- clothing
- hats or non-hair accessories

## 8. Formula Rule

The formula-spec definition already fixed in the project is:

\[
HeadROI = Dilate(FaceRegionMask, r_{head}) \cap BioMask
\]

\[
HairCandidateMask = (BioMask \setminus FaceRegionMask) \cap HeadROI
\]

\[
HairMask =
HairCandidateMask
\cdot (1 - SkinProb)
\cdot HairTextureProb
\]

\[
HairMask = ConnectedTo(HairMask,\ HeadROI)
\]

\[
HairAlpha = HairMask \cdot SmoothStep(s_h,e_h,d_{hair})
\]

## 9. Separation Rule

`HairMask` must stay separate from:

- `FaceSkinMask`
- `EyebrowMask`
- `EyelashMask`
- `FacialHairMask`
- `AccessoryMask`

Important meaning split:

- `HairMask` = scalp hair preserve region
- `FacialHairMask` = beard / mustache / sideburn family
- `EyebrowMask` and `EyelashMask` = separate eye-area hair structures

Do not silently expand `HairMask` into all visible hair-like regions.

## 10. Output Contract

Preferred result structure:

`HairMaskBuildResult`

Suggested fields:

- `Status`
- `Confidence`
- `HairCandidateMask`
- `HairMask`
- `HairAlpha`
- `HeadROI`
- `Warnings`

Minimum required outputs:

- `HairMask`
- optional `HairAlpha`

## 11. Downstream Use Rule

Use `HairMask` for:
- hair preservation during background replacement
- hair-only retouch routing
- head-bio and person-mask assembly

Do not use `HairMask` as:
- a beard mask
- an eyebrow mask
- a skin exclusion shortcut by itself

Hair-only color work should derive a narrower target such as:

\[
HairColorTargetMask =
HairMask
\cdot (1 - FaceSkinMask)
\cdot (1 - EarMask)
\cdot (1 - EyebrowMask)
\cdot (1 - EyelashMask)
\cdot (1 - FacialHairMask)
\cdot (1 - AccessoryMask)
\]

## 12. Debug Outputs

Recommended debug outputs:

- `debug_hair_candidate_mask.png`
- `debug_hair_mask.png`
- `debug_hair_alpha.png`
- `debug_hair_mask_overlay.png`
- `debug_hair_mask_report.json`

Recommended report fields:

- build status
- confidence
- head ROI size
- connection rule result
- warnings

## 13. Do-Not-Do Rules

- Do not merge eyebrow or eyelash regions into `HairMask`.
- Do not merge beard or mustache into `HairMask`.
- Do not let face skin spill into `HairMask`.
- Do not let hat or accessory edges become hair by default.
- Do not collapse soft hair edge behavior into a hard binary shape if alpha is available.

## 14. Success Condition

`BuildHairMask(...)` is successful when:

- visible scalp hair is preserved as its own stable region
- face skin, brows, lashes, and facial hair remain separate
- the result supports both hair-edge compositing and hair-only tools
- the result feeds `HeadBioMask`, `PersonMask`, and `PersonAlpha` safely

## 15. One-Line Definition

`BuildHairMask(...)` constructs the stable scalp-hair preserve mask from head-side hair candidates so that portrait compositing and hair-only retouch can keep visible head hair separate from skin, eye hair, facial hair, and accessories.
