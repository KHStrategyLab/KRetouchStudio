# BuildClothingMask Contract

## 1. Purpose

`BuildClothingMask(...)` defines the first stable worn-clothing mask contract for KRetouchPro.

Its role is:
- isolate clothing ownership from biological body ownership
- provide the preserve-side garment region for `PersonMask`
- support clothing protection and clothing-only edits without leaking into skin or background

This function is not a fashion classifier.
It builds the worn-garment ownership layer used by person-preserve and clothing tools.

## 2. Current Stage Definition

At the current project stage:

- `ClothingMask` must remain separate from `BioMask`
- lower clothing boundary and neckline interpretation already matter for portrait work
- `PersonMask` needs clothing preserved, not re-invented later during compositing

Therefore `BuildClothingMask(...)` is defined as:

**the mask-construction step that turns garment-side regions into a stable worn-clothing preserve mask**

It is not:
- a background mask builder
- a full garment attribute engine
- a skin detector
- a person classifier from scratch

## 3. Function Name

Preferred name:

`BuildClothingMask(...)`

Related later functions:

- `DetectClothingBoundary(...)`
- `BuildPersonMask(...)`
- `BuildBackgroundMask(...)`
- `BuildSubjectMatte(...)`
- `BuildClothingBoundaryWorkArea(...)`
- `ApplyClothingTone(...)`

## 4. Ownership

Pillar:

- Mask Construction

This function should construct worn-garment ownership only.
It must not directly:
- replace the background
- retouch the skin
- redefine biological regions as garments
- merge background into clothing
- decide handheld-object preservation

## 5. Primary Use Cases

### 5.1 Person preservation

Use `ClothingMask` as part of final `PersonMask` so garments do not disappear during background replacement.

### 5.2 Clothing protection

Use `ClothingMask` to keep skin-only tools from leaking onto fabric.

### 5.3 Clothing-only operations

Use `ClothingMask` later for:

- clothing tone
- clothing recolor
- garment cleanup
- neckline-aware protection

## 6. Input Contract

The builder should accept the following inputs.

### 6.1 Required

- `TopGarmentMask`
- `LowerGarmentMask`
- `FootwearMask`
- `SockMask`
- `GarmentAccessoryMask`
- `ImageWidth`
- `ImageHeight`

### 6.2 Recommended

- `DetectedClothingBoundary`
- `NecklineRegion`
- `UpperBodyClothingROI`
- `LowerBodyClothingROI`
- `BioMask`
- `SkinMask`
- `AccessoryMask`

### 6.3 Optional

- `BuildMode`
  - `Preserve`
  - `ClothingEdit`
  - `DebugOnly`
- `ClothingAlphaMode`
- `LayerPolicy`

## 7. Core Meaning Rule

`ClothingMask` means worn garment ownership.

It should include:
- upper garments
- lower garments
- footwear
- socks
- garment-bound attached clothing accessories

It should exclude:
- skin
- scalp hair
- eyebrows
- eyelashes
- facial hair
- non-worn props
- background

It should stay separate from non-garment worn accessories when those belong to `AccessoryMask`.

## 8. Formula Rule

The formula-spec definition already fixed in the project is:

\[
ClothingMask =
TopGarmentMask
\cup LowerGarmentMask
\cup FootwearMask
\cup SockMask
\cup GarmentAccessoryMask
\]

Current upper-body garment sub-definition:

\[
TopGarmentMask = ClothingMask \cap UpperBodyClothingROI
\]

Current lower-garment grouping:

\[
LowerGarmentMask =
PantsMask
\cup SkirtMask
\cup ShortsMask
\cup LeggingsMask
\]

Companion alpha when needed:

\[
ClothingAlpha = ClothingMask \cdot SmoothStep(s_c,e_c,d_c)
\]

## 9. Layer Rule

Clothing must respect the existing dictionary layer rule:

- a jacket can sit over a shirt
- a shirt can sit over skin
- a belt can sit over pants or skirt

Do not overwrite body-region labels with clothing labels.
Use clothing masks as separate overlay ownership.

## 10. Separation Rule

`ClothingMask` must stay separate from:

- `BioMask`
- `SkinMask`
- `HairMask`
- `AccessoryMask`
- `BackgroundMask`

Important meaning split:

- `ClothingMask` = worn garment ownership
- `AccessoryMask` = worn non-garment accessory ownership
- `BioMask` = biological body ownership

Do not put clothing into `BioMask`.
Do not use `ClothingMask` as a shortcut for all non-skin pixels.

## 11. Output Contract

Preferred result structure:

`ClothingMaskBuildResult`

Suggested fields:

- `Status`
- `Confidence`
- `ClothingMask`
- `ClothingAlpha`
- `TopGarmentMask`
- `LowerGarmentMask`
- `FootwearMask`
- `SockMask`
- `GarmentAccessoryMask`
- `Warnings`

Minimum required outputs:

- `ClothingMask`
- optional `ClothingAlpha`

## 12. Downstream Use Rule

Use `ClothingMask` for:
- person-mask assembly
- clothing preservation
- clothing-only edits
- block/protect routing against skin tools

Do not use `ClothingMask` as:
- a body mask
- a background mask
- a handheld-object mask

## 13. Debug Outputs

Recommended debug outputs:

- `debug_clothing_mask.png`
- `debug_clothing_alpha.png`
- `debug_top_garment_mask.png`
- `debug_lower_garment_mask.png`
- `debug_clothing_mask_overlay.png`
- `debug_clothing_mask_report.json`

Recommended report fields:

- build status
- confidence
- neckline / boundary support used
- garment-layer warnings
- warnings

## 14. Do-Not-Do Rules

- Do not merge visible skin into `ClothingMask`.
- Do not let background become clothing in low-confidence regions.
- Do not confuse shirt collar, neckline, or lapel with neck skin.
- Do not silently treat all worn accessories as garment ownership.
- Do not use clothing segmentation to overwrite anatomy ownership.

## 15. Success Condition

`BuildClothingMask(...)` is successful when:

- worn garments are preserved as their own stable region
- skin and body ownership stay separate
- clothing can feed `PersonMask` without later rescue logic
- clothing-only and skin-only tools can route without semantic overlap

## 16. One-Line Definition

`BuildClothingMask(...)` constructs the stable worn-clothing preserve mask from garment-side regions so that person preservation, clothing protection, and clothing-only edits stay separate from body, hair, accessories, and background.
