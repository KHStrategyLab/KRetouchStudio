# BuildBackgroundMask Contract

## 1. Purpose

`BuildBackgroundMask(...)` defines the first stable non-person mask contract for KRetouchPro.

Its role is:
- derive the background-side mask from the final person-preserve result
- support background replacement and background-only operations
- keep non-person meaning separate from skin and subject-preserve meaning

This function is not the primary person detector.
It is the background-side construction step that follows person-preserve definition.

## 2. Current Stage Definition

At the current project stage:

- the full selected working image is the base domain `1`
- `PersonMask` is the preserve-side anchor for background replacement
- `BackgroundMask` should follow `PersonMask` or `PersonAlpha`, not replace their meaning
- `SkinMask` remains a separate retouch-only axis

Therefore `BuildBackgroundMask(...)` is defined as:

**the mask-construction step that derives the non-person background region from the final person-preserve boundary**

It is not:
- a skin detector
- a clothing detector
- a person classifier from scratch
- a full background-edit pipeline by itself

## 3. Function Name

Preferred name:

`BuildBackgroundMask(...)`

Related later functions:

- `BuildPersonMask(...)`
- `BuildSubjectMatte(...)`
- `DetectExposedBackgroundGap(...)`
- `BlendBackground(...)`
- `HealSeam(...)`

## 4. Ownership

Pillar:

- Background / Matte Compositing

This function should construct the non-person side of the composite.
It must not directly:
- retouch skin
- detect clothing boundaries
- warp geometry
- redefine person ownership

## 5. Primary Use Cases

### 5.1 Background replacement

Use `BackgroundMask` to determine where the new background may appear.

### 5.2 Background-only adjustment

Use `BackgroundMask` for:

- background blur
- background recolor
- background cleanup
- background noise or tone control

### 5.3 Composite routing

Use `BackgroundMask` and `PersonAlpha` together for:

- foreground/background blending
- seam correction
- edge stabilization

## 6. Input Contract

The builder should accept the following inputs.

### 6.1 Required

- `PersonMask`
  - for binary background separation
or
- `PersonAlpha`
  - for soft compositing
- `ImageWidth`
- `ImageHeight`

### 6.2 Recommended

- `SubjectMask`
  - only if a later feature preserves handheld objects
- `HairAlpha`
- `PreviewOrExportMode`
- `WorkArea`

### 6.3 Optional

- `ExposedBackgroundGapMask`
- `BackgroundBuildMode`
  - `Binary`
  - `SoftAlpha`
  - `CompositeOnly`
  - `DebugOnly`

## 7. Core Meaning Rule

`BackgroundMask` means visible non-person background.

It is the non-person remainder inside the full image domain `1`.

It must include:
- studio backdrop
- wall
- floor
- empty negative space
- real visible gaps that connect to background

It must exclude:
- person body
- hair
- clothing
- shoes
- worn accessories

By default it should also exclude handheld objects when those objects are intentionally preserved through `SubjectMask`.

## 8. Formula Rule

For soft compositing, the preferred definition is:

\[
BackgroundMask = 1 - PersonAlpha
\]

For binary routing when alpha is not used:

\[
BackgroundMask = 1 - PersonMask
\]

This means `BackgroundMask` is always derived inside `ImageDomain = 1`.

Background replacement composite:

\[
Composite =
Foreground \cdot PersonAlpha
+
NewBackground \cdot (1 - PersonAlpha)
\]

## 9. Real-Gap Rule

Because `PersonMask` fills only small enclosed mask errors, `BackgroundMask` must keep true visible background gaps.

These remain background:
- finger gaps
- crossed-arm gaps
- arm-body negative spaces
- open spaces not owned by person, clothing, or worn accessories

This is not a bug.
It is part of correct person/non-person separation.

## 10. Separation Rule

`BackgroundMask` must be built from the person-preserve axis, not from the skin-retouch axis.

Important separation:

- `PersonMask` = preserve the person
- `BackgroundMask` = everything outside preserved person
- `SkinMask` = skin-only retouch target

Do not derive `BackgroundMask` from `SkinMask`.

## 11. Output Contract

Preferred result structure:

`BackgroundMaskBuildResult`

Suggested fields:

- `Status`
- `Confidence`
- `BackgroundMask`
- `BackgroundAlpha`
- `Warnings`

Minimum required output:

- `BackgroundMask`

## 12. Downstream Use Rule

Use `BackgroundMask` for:
- background replacement
- background-only filters
- block-side mask routing for non-person edits

Do not use `BackgroundMask` as:
- a skin exclusion shortcut
- a clothing detector
- a person-side preserve mask

## 13. Debug Outputs

Recommended debug outputs:

- `debug_background_mask.png`
- `debug_background_mask_overlay.png`
- `debug_person_alpha.png`
- `debug_background_replace_preview.png`
- `debug_background_mask_report.json`

Recommended report fields:

- build status
- alpha mode
- preserve source
  - `PersonMask`
  - `PersonAlpha`
  - `SubjectMask`
- warnings

## 14. Do-Not-Do Rules

- Do not treat parsing-background seed alone as the final background truth.
- Do not redefine person ownership inside this function.
- Do not let background overwrite preserved hair edges.
- Do not use `BackgroundMask` as a substitute for `ClothingMask`.
- Do not merge skin logic into background logic.

## 15. Success Condition

`BuildBackgroundMask(...)` is successful when:

- new background appears only outside the preserved person
- hair, clothing, and worn accessories stay on the preserve side
- true visible background gaps remain background
- background routing remains clearly separate from `SkinMask`

## 16. One-Line Definition

`BuildBackgroundMask(...)` constructs the non-person background remainder inside the full image domain `1` from the final person-preserve boundary so that background replacement and background-only operations stay outside the preserved visible person.
