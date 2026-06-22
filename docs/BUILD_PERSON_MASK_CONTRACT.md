# BuildPersonMask Contract

## 1. Purpose

`BuildPersonMask(...)` defines the first stable person-preservation mask contract for KRetouchPro.

Its role is:
- combine biological, clothing, and worn-accessory ownership into one final visible-person mask
- provide the preserve-side mask for background replacement
- keep person meaning separate from skin-only meaning

This function does not detect the whole image from scratch.
It resolves already-defined person-side mask ownership into one final mask.

## 2. Current Stage Definition

At the current project stage:

- the full selected working image is the base domain `1`
- `PersonMask` and `SkinMask` must stay separate
- background replacement depends on a correct person-preserve boundary
- clothing, hair, and worn accessories must survive as part of the visible person

Therefore `BuildPersonMask(...)` is defined as:

**the mask-construction step that turns person-side component masks into the final visible-person preserve mask**

It is not:
- a skin mask builder
- a clothing detector
- a handheld-object extender
- a background replacer

## 3. Function Name

Preferred name:

`BuildPersonMask(...)`

Related later functions:

- `BuildBioMask(...)`
- `BuildClothingMask(...)`
- `BuildAccessoryMask(...)`
- `BuildSubjectMask(...)`
- `BuildBackgroundMask(...)`
- `BuildSubjectMatte(...)`

## 4. Ownership

Pillar:

- Mask Construction

This function should construct the final preserve-side person mask.
It must not directly:
- retouch pixels
- detect landmarks
- perform geometry warp
- smooth skin
- replace the background

## 5. Primary Use Cases

### 5.1 Background replacement

Use `PersonMask` and companion `PersonAlpha` to preserve the visible person while replacing the background.

### 5.2 Final subject preservation

Use `PersonMask` to ensure the final outer person shape keeps:

- hair
- face
- neck
- clothing
- shoes
- worn accessories

### 5.3 Matte and edge routing

Use `PersonMask` as the preserve-side parent for:

- `BuildBackgroundMask(...)`
- `BuildSubjectMatte(...)`
- edge feathering
- background seam healing

## 6. Input Contract

The builder should accept the following inputs.

### 6.1 Required

- `BioMask`
- `ClothingMask`
- `AccessoryMask`
- `ImageWidth`
- `ImageHeight`

### 6.2 Recommended

- `BioAlpha`
- `ClothingAlpha`
- `AccessoryAlpha`
- `HairAlpha`
- `FaceBox`
- `DetectedVisiblePersonRegion`

### 6.3 Optional

- `BuildMode`
  - `BackgroundReplace`
  - `SubjectPreserve`
  - `DebugOnly`
- `HoleFillPolicy`
- `MaxInternalHoleArea`
- `HandheldObjectMask`
  - only for later `SubjectMask`, not for default `PersonMask`

## 7. Core Meaning Rule

`PersonMask` means the final visible preserved person.

It is defined inside the full image domain `1`.

It must include:
- biological body regions from `BioMask`
- scalp hair and face hair already owned by `BioMask`
- clothing
- shoes
- worn accessories

It must exclude:
- background
- cast shadows
- chair or studio furniture
- nearby props
- another person
- handheld non-worn objects by default

## 8. Formula Rule

The formula-spec definition already fixed in the project is:

\[
PersonMask \subset 1
\]

\[
PersonMaskRaw = BioMask \cup ClothingMask \cup AccessoryMask
\]

soft union form:

\[
PersonMaskRaw = Clip01(BioMask + ClothingMask + AccessoryMask)
\]

or:

\[
PersonMaskRaw = \max(BioMask,\ ClothingMask,\ AccessoryMask)
\]

final mask:

\[
PersonMask = FillSmallInternalHoles(PersonMaskRaw,\ maxArea,\ enclosedOnly)
\]

companion alpha when needed for compositing:

\[
PersonAlpha = \max(BioAlpha,\ ClothingAlpha,\ AccessoryAlpha,\ HairAlpha)
\]

## 9. Internal-Hole Rule

`FillSmallInternalHoles(...)` must only fill small enclosed mask errors.

It must not fill true background structures such as:
- crossed-arm gaps
- finger gaps
- arm-to-body visible gaps
- neckline openings where real background is visible

If a gap is connected to real background, it remains background.

## 10. Separation Rule

`PersonMask` must not collapse into `SkinMask`.

Important separation:

- `PersonMask` = visible person preservation
- `SkinMask` = skin-only retouch target

Do not use `PersonMask` directly as the final skin-retouch apply mask.

## 11. Output Contract

Preferred result structure:

`PersonMaskBuildResult`

Suggested fields:

- `Status`
- `Confidence`
- `PersonMaskRaw`
- `PersonMask`
- `PersonAlpha`
- `Warnings`

Minimum required outputs:

- `PersonMask`
- optional `PersonAlpha`

## 12. Downstream Use Rule

Use `PersonMask` for:
- background replacement
- subject preservation
- person-side composite routing

Use `SubjectMask` only when a later feature explicitly adds:
- `HandheldObjectMask`

Do not silently extend `PersonMask` into `SubjectMask`.

## 13. Debug Outputs

Recommended debug outputs:

- `debug_person_mask_raw.png`
- `debug_person_mask_final.png`
- `debug_person_alpha.png`
- `debug_person_mask_overlay.png`
- `debug_person_mask_report.json`

Recommended report fields:

- build status
- confidence
- hole-fill policy
- internal-hole count
- warnings

## 14. Do-Not-Do Rules

- Do not define `PersonMask` as biological-only.
- Do not let clothing drop out of the final preserve mask.
- Do not treat worn accessories as background by default.
- Do not add handheld props unless the feature explicitly requests `SubjectMask`.
- Do not reuse `SkinMask` as a shortcut for `PersonMask`.

## 15. Success Condition

`BuildPersonMask(...)` is successful when:

- the visible person remains fully preserved for background replacement
- clothing and worn accessories are not lost
- real background gaps remain background
- the result stays clearly distinct from `SkinMask`

## 16. One-Line Definition

`BuildPersonMask(...)` constructs the final visible-person preserve mask inside the full image domain `1` by combining biological, clothing, and worn-accessory ownership into a single person-side mask for background replacement and subject preservation.
