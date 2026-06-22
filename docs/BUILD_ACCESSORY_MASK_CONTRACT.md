# BuildAccessoryMask Contract

## 1. Purpose

`BuildAccessoryMask(...)` defines the first stable worn-accessory mask contract for KRetouchPro.

Its role is:
- isolate worn non-garment accessory ownership from body and clothing ownership
- preserve accessory silhouette during background replacement
- support accessory protection and accessory-only operations

This function is not a generic object detector.
It builds the worn-accessory ownership layer used by person preservation.

## 2. Current Stage Definition

At the current project stage:

- `AccessoryMask` belongs to the preserve side of `PersonMask`
- `AccessoryMask` must stay separate from `ClothingMask`
- `AccessoryMask` must stay separate from `BioMask` and `SkinMask`

Therefore `BuildAccessoryMask(...)` is defined as:

**the mask-construction step that turns worn-accessory regions into a stable accessory preserve mask**

It is not:
- a handheld object detector
- a clothing mask builder
- a skin detector
- a background mask builder

## 3. Function Name

Preferred name:

`BuildAccessoryMask(...)`

Related later functions:

- `BuildPersonMask(...)`
- `BuildSubjectMask(...)`
- `BuildBackgroundMask(...)`
- `BuildSubjectMatte(...)`
- `AccessoryProtectMask`

## 4. Ownership

Pillar:

- Mask Construction

This function should construct worn-accessory ownership only.
It must not directly:
- recolor garments
- smooth skin
- redefine body ownership
- merge handheld props into worn accessories
- replace the background

## 5. Primary Use Cases

### 5.1 Person preservation

Use `AccessoryMask` as part of `PersonMask` so glasses, hat, earrings, and similar items survive background replacement.

### 5.2 Accessory protection

Use `AccessoryMask` to keep skin or hair operations from contaminating accessory regions.

### 5.3 Accessory-only routing

Use `AccessoryMask` later for:

- accessory protection
- accessory tone correction
- accessory cleanup

## 6. Input Contract

The builder should accept the following inputs.

### 6.1 Required

- `GlassesMask`
- `HatMask`
- `EarringMask`
- `NecklaceMask`
- `BraceletMask`
- `WatchMask`
- `RingMask`
- `HairAccessoryMask`
- `ImageWidth`
- `ImageHeight`

### 6.2 Recommended

- `AccessoryCandidate`
- `FaceRegionMask`
- `HeadTopROI`
- `EyeNoseBridgeROI`
- `HairMask`
- `ClothingMask`
- `BioMask`

### 6.3 Optional

- `BuildMode`
  - `Preserve`
  - `Protect`
  - `DebugOnly`
- `AccessoryAlphaMode`
- `WearabilityConfidence`

## 7. Core Meaning Rule

`AccessoryMask` means worn non-garment accessory ownership.

It should include:
- glasses
- hat
- earrings
- necklace
- bracelet
- watch
- ring
- hair accessories

It should exclude:
- clothing
- skin
- scalp hair
- beard
- held props
- background

## 8. Formula Rule

The formula-spec definition already fixed in the project is:

\[
AccessoryMask =
GlassesMask
\cup HatMask
\cup EarringMask
\cup NecklaceMask
\cup BraceletMask
\cup WatchMask
\cup RingMask
\cup HairAccessoryMask
\]

Current example component definitions already include:

\[
GlassesMask = FaceRegionMask \cap EyeNoseBridgeROI \cdot GlassesProb
\]

\[
HatMask = HeadTopROI \cap AccessoryProb \cdot HatShapeProb
\]

Companion alpha when needed:

\[
AccessoryAlpha = AccessoryMask \cdot SmoothStep(s_a,e_a,d_a)
\]

## 9. Separation Rule

`AccessoryMask` must stay separate from:

- `ClothingMask`
- `BioMask`
- `SkinMask`
- `HairMask`
- `HandheldObjectMask`

Important meaning split:

- `AccessoryMask` = worn non-garment accessory ownership
- `ClothingMask` = worn garment ownership
- `HandheldObjectMask` = carried non-worn object ownership

Do not silently move carried props into `AccessoryMask`.

## 10. Output Contract

Preferred result structure:

`AccessoryMaskBuildResult`

Suggested fields:

- `Status`
- `Confidence`
- `AccessoryMask`
- `AccessoryAlpha`
- component accessory masks
- `Warnings`

Minimum required outputs:

- `AccessoryMask`
- optional `AccessoryAlpha`

## 11. Downstream Use Rule

Use `AccessoryMask` for:
- person-mask assembly
- accessory protection
- block/protect routing for skin and hair tools

Do not use `AccessoryMask` as:
- a garment mask
- a carried-object mask
- a body mask

## 12. Debug Outputs

Recommended debug outputs:

- `debug_accessory_mask.png`
- `debug_accessory_alpha.png`
- `debug_accessory_mask_overlay.png`
- `debug_accessory_mask_report.json`

Recommended report fields:

- build status
- confidence
- worn-accessory component availability
- warnings

## 13. Do-Not-Do Rules

- Do not merge hats into scalp hair by default.
- Do not merge glasses into face skin.
- Do not merge necklaces or bracelets into clothing.
- Do not treat held phone, book, or bag as worn accessory unless explicitly modeled as worn.
- Do not let low-confidence accessory guesses overwrite body or clothing ownership.

## 14. Success Condition

`BuildAccessoryMask(...)` is successful when:

- worn accessories are preserved as their own stable region
- body, clothing, and carried props remain separate
- the result can feed `PersonMask` and protect accessory regions during retouch

## 15. One-Line Definition

`BuildAccessoryMask(...)` constructs the stable worn-accessory preserve mask from non-garment accessory regions so that person preservation and local retouch can keep accessories separate from body, clothing, hair, and held props.
