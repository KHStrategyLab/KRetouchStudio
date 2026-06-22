# BuildHandheldObjectMask Contract

## 1. Purpose

`BuildHandheldObjectMask(...)` defines the first stable carried-object mask contract for KRetouchPro.

Its role is:
- isolate non-worn objects intentionally held by the person
- support optional extension from `PersonMask` to `SubjectMask`
- keep carried objects separate from clothing, accessories, and background

This function is optional at the current project stage.
It becomes active only when a feature explicitly wants held objects preserved with the subject.

## 2. Current Stage Definition

At the current project stage:

- default `PersonMask` must not include held objects
- `SubjectMask` may include held objects only when explicitly requested
- held objects need their own meaning instead of being silently folded into accessories or clothing

Therefore `BuildHandheldObjectMask(...)` is defined as:

**the optional mask-construction step that isolates intentionally carried non-worn objects for subject extension**

It is not:
- a default part of `PersonMask`
- a worn-accessory mask builder
- a clothing mask builder
- a generic scene object detector for everything in the frame

## 3. Function Name

Preferred name:

`BuildHandheldObjectMask(...)`

Related later functions:

- `BuildSubjectMask(...)`
- `BuildPersonMask(...)`
- `BuildBackgroundMask(...)`
- `BuildSubjectMatte(...)`

## 4. Ownership

Pillar:

- Mask Construction

This function should construct carried-object ownership only.
It must not directly:
- redefine person ownership
- redefine clothing or accessory ownership
- replace the background
- smooth or warp the image

## 5. Primary Use Cases

### 5.1 Subject extension

Use `HandheldObjectMask` only when the workflow explicitly preserves carried objects with the subject.

### 5.2 Special extraction

Use `HandheldObjectMask` for:

- prop-inclusive cutout
- held product preview
- carried-item preserve composites

### 5.3 Debug ownership separation

Use `HandheldObjectMask` to keep:

- carried props
- worn accessories
- clothing
- background

from collapsing into one ambiguous non-skin region.

## 6. Input Contract

The builder should accept the following inputs.

### 6.1 Required

- `SourceImage`
- `ImageWidth`
- `ImageHeight`
- `HandRegionGuide`
  - at least one hand-side or hand-contact guide

### 6.2 Recommended

- `HandMask`
- `ArmMask`
- `SubjectContactRegion`
- `AccessoryMask`
- `ClothingMask`
- `BackgroundMask`

### 6.3 Optional

- `BuildMode`
  - `SubjectExtend`
  - `DebugOnly`
- `ObjectClassHint`
  - `Phone`
  - `Book`
  - `Bag`
  - `Document`
  - `Unknown`
- `PreservePolicy`

## 7. Core Meaning Rule

`HandheldObjectMask` means a visible non-worn object intentionally carried or held by the person.

It should include:
- phone in hand
- book in hand
- folder or paper bundle in hand
- held bag handle/object when explicitly treated as held object

It should exclude:
- worn accessories
- clothing
- body parts
- background
- nearby props not actually connected to the person

## 8. Formula Rule

The current project formula does not define a separate fixed closed-form expression yet.

At this stage the contract rule is:

\[
SubjectMask = PersonMask \cup HandheldObjectMask
\]

Therefore `HandheldObjectMask` is:

**an optional add-on ownership mask that may extend the preserved subject beyond the default person boundary**

## 9. Contact Rule

The object should not be included just because it is near the person.

Preferred logic:

1. object candidate near hand or hand-contact region
2. visible connection or intentional carry evidence
3. non-worn classification
4. explicit preserve request

Without enough evidence, return warning output instead of forced inclusion.

## 10. Separation Rule

`HandheldObjectMask` must stay separate from:

- `PersonMask`
- `AccessoryMask`
- `ClothingMask`
- `BackgroundMask`

Important meaning split:

- default `PersonMask` = person only
- optional `SubjectMask` = person plus explicit carried object

Do not silently put held props into default `PersonMask`.

## 11. Output Contract

Preferred result structure:

`HandheldObjectMaskBuildResult`

Suggested fields:

- `Status`
- `Confidence`
- `HandheldObjectMask`
- `ObjectClassHint`
- `Warnings`

Minimum required output:

- `HandheldObjectMask`

## 12. Downstream Use Rule

Use `HandheldObjectMask` for:
- optional `SubjectMask` extension
- prop-inclusive subject extraction
- explicit held-object preserve features

Do not use `HandheldObjectMask` as:
- worn accessory ownership
- clothing ownership
- generic clutter collection mask

## 13. Debug Outputs

Recommended debug outputs:

- `debug_handheld_object_mask.png`
- `debug_handheld_object_mask_overlay.png`
- `debug_handheld_object_mask_report.json`

Recommended report fields:

- build status
- confidence
- contact evidence
- preserve decision
- warnings

## 14. Do-Not-Do Rules

- Do not add held objects to `PersonMask` by default.
- Do not merge watches, rings, or earrings into carried-object ownership.
- Do not include nearby furniture or background objects just because they touch the crop.
- Do not create a large uncertain mask when hand-contact evidence is weak.

## 15. Success Condition

`BuildHandheldObjectMask(...)` is successful when:

- a real carried object can be preserved without changing default person meaning
- worn accessories and clothing remain separate
- optional `SubjectMask` extension becomes explicit and controllable

## 16. One-Line Definition

`BuildHandheldObjectMask(...)` constructs the optional carried-object mask for visible non-worn items intentionally held by the person, so that subject extraction can extend beyond the default person boundary only when explicitly requested.
