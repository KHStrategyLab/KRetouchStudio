# BuildSubjectMatte Contract

## 1. Purpose

`BuildSubjectMatte(...)` defines the first stable subject-matte construction contract for KRetouchPro.

Its role is:
- convert preserve-side mask ownership into final compositing alpha
- prepare stable foreground/background separation for preview and export
- keep compositing logic separate from detection and mask meaning

This function does not decide what the person is from scratch.
It consumes already-defined mask ownership and turns that into matte-ready output.

## 2. Current Stage Definition

At the current project stage:

- `BuildPersonMask(...)` owns final person-preserve boundary meaning
- `BuildBackgroundMask(...)` owns non-person background meaning
- the next composition step needs a stable matte and alpha result

Therefore `BuildSubjectMatte(...)` is defined as:

**the compositing-preparation step that turns person-preserve masks and alphas into a stable foreground matte package**

It is not:
- a landmark detector
- a skin-retouch mask builder
- a clothing classifier
- a full background editing pipeline by itself

## 3. Function Name

Preferred name:

`BuildSubjectMatte(...)`

Related later functions:

- `BuildPersonMask(...)`
- `BuildBackgroundMask(...)`
- `BlendBackground(...)`
- `FeatherMaskEdge(...)`
- `MatchEdgeTone(...)`
- `HealSeam(...)`

## 4. Ownership

Pillar:

- Background / Matte Compositing

This function should prepare matte-ready preserve data.
It must not directly:
- redefine `PersonMask`
- redefine `BackgroundMask`
- smooth skin
- warp geometry
- invent new region ownership

## 5. Primary Use Cases

### 5.1 Preview compositing

Use `BuildSubjectMatte(...)` to prepare preview-safe foreground/background separation.

### 5.2 Export compositing

Use `BuildSubjectMatte(...)` to prepare export-side alpha and matte outputs for final rendering.

### 5.3 Edge-aware routing

Use the subject matte package to support:

- feathered hair boundary handling
- background seam healing
- edge tone matching

## 6. Input Contract

The builder should accept the following inputs.

### 6.1 Required

- `PersonMask`
- `PersonAlpha`
- `BackgroundMask`
- `SourceImage`
- `ImageWidth`
- `ImageHeight`

### 6.2 Recommended

- `HairAlpha`
- `BuildBackgroundMaskResult`
- `BuildPersonMaskResult`
- `EdgeDistanceField`
- `PreviewOrExportMode`

### 6.3 Optional

- `SubjectMask`
- `HandheldObjectMask`
- `FeatherRadius`
- `MatteBuildMode`
  - `Preview`
  - `Export`
  - `DebugOnly`

## 7. Core Meaning Rule

`SubjectMatte` means the compositing package that preserves the visible subject side against the background side.

It should preserve:
- final visible person silhouette
- soft hair edges
- person-side semi-transparent edge transitions

It should separate:
- preserve-side foreground
- replaceable background

It should not redefine:
- skin-only targets
- clothing-only targets
- feature masks

## 8. Formula Rule

The project already fixed the preserve alpha rule:

\[
PersonAlpha = \max(BioAlpha,\ ClothingAlpha,\ AccessoryAlpha,\ HairAlpha)
\]

The background-side rule is:

\[
BackgroundMask = 1 - PersonAlpha
\]

The composite rule is:

\[
Composite(x,y) =
Foreground(x,y) \cdot PersonAlpha(x,y)
+
NewBackground(x,y) \cdot (1 - PersonAlpha(x,y))
\]

`BuildSubjectMatte(...)` should prepare the data needed for this composition.

## 9. Matte Construction Rule

The builder should:

1. accept preserve-side mask ownership and alpha
2. keep hair-edge softness on the foreground side
3. keep real background-side gaps on the background side
4. avoid hard binary collapse when a soft alpha edge is already available
5. return a matte package ready for preview or export compositing

## 10. Separation Rule

`BuildSubjectMatte(...)` must stay downstream of mask meaning.

Important separation:

- `BuildPersonMask(...)` decides preserve ownership
- `BuildBackgroundMask(...)` decides non-person ownership
- `BuildSubjectMatte(...)` prepares compositing output from those decisions

Do not move person/background meaning decisions into matte construction.

## 11. Output Contract

Preferred result structure:

`SubjectMatteBuildResult`

Suggested fields:

- `Status`
- `Confidence`
- `PersonAlpha`
- `BackgroundAlpha`
- `ForegroundMatte`
- `CompositeReadyMask`
- `Warnings`

Minimum required outputs:

- `PersonAlpha`
- matte-ready preserve/background separation

## 12. Downstream Use Rule

Use `BuildSubjectMatte(...)` for:
- preview composite preparation
- export composite preparation
- edge-aware background replacement

Do not use it as:
- a substitute for `BuildPersonMask(...)`
- a substitute for `BuildBackgroundMask(...)`
- a substitute for `SkinMask`

## 13. Debug Outputs

Recommended debug outputs:

- `debug_subject_matte.png`
- `debug_person_alpha.png`
- `debug_background_alpha.png`
- `debug_subject_matte_overlay.png`
- `debug_composite_preview.png`
- `debug_subject_matte_report.json`

Recommended report fields:

- build status
- confidence
- preserve alpha source
- background alpha source
- preview/export mode
- warnings

## 14. Do-Not-Do Rules

- Do not collapse a valid soft hair edge into a hard binary cutout.
- Do not invent new preserve ownership inside matte construction.
- Do not use matte generation to hide bad person-mask decisions.
- Do not let background-side logic override preserve-side clothing or accessory ownership.
- Do not mix skin-retouch targeting into foreground/background matte rules.

## 15. Success Condition

`BuildSubjectMatte(...)` is successful when:

- preview and export have a stable person-side alpha
- hair edges remain compositing-safe
- real background gaps remain on the background side
- foreground/background preparation stays separate from mask-definition logic

## 16. One-Line Definition

`BuildSubjectMatte(...)` constructs the foreground/background matte package from already-defined person-preserve masks and alphas so that preview and export compositing can preserve the visible subject with stable soft edges.
