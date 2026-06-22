# BuildSubjectMask Contract

## 1. Purpose

`BuildSubjectMask(...)` defines the first stable extended-subject mask contract for KRetouchPro.

Its role is:
- preserve the default visible person
- optionally extend subject preservation to explicitly held objects
- provide the subject-side ownership used when the workflow wants more than person-only extraction

This function is downstream of `BuildPersonMask(...)`.
It should not replace person-mask meaning.

## 2. Current Stage Definition

At the current project stage:

- default extraction uses `PersonMask`
- optional held-object preservation uses `SubjectMask`
- the extension must stay explicit instead of silently changing person meaning

Therefore `BuildSubjectMask(...)` is defined as:

**the mask-construction step that builds the extended preserved subject from `PersonMask` plus optional carried-object ownership**

It is not:
- a replacement for `BuildPersonMask(...)`
- a skin-retouch mask builder
- a clothing detector
- a scene segmentation engine

## 3. Function Name

Preferred name:

`BuildSubjectMask(...)`

Related later functions:

- `BuildPersonMask(...)`
- `BuildHandheldObjectMask(...)`
- `BuildBackgroundMask(...)`
- `BuildSubjectMatte(...)`

## 4. Ownership

Pillar:

- Mask Construction

This function should construct extended subject ownership only.
It must not directly:
- redefine default person ownership
- redefine background ownership
- warp geometry
- retouch pixels

## 5. Primary Use Cases

### 5.1 Prop-inclusive extraction

Use `SubjectMask` when the desired output must preserve the person together with an intentionally held object.

### 5.2 Special compositing

Use `SubjectMask` for:

- person + held-object cutout
- prop-inclusive background replacement
- held-product or held-document composites

### 5.3 Explicit preserve routing

Use `SubjectMask` only when the workflow explicitly asks for subject extension beyond person-only preservation.

## 6. Input Contract

The builder should accept the following inputs.

### 6.1 Required

- `PersonMask`
- `ImageWidth`
- `ImageHeight`

### 6.2 Recommended

- `HandheldObjectMask`
- `SubjectExtendEnabled`
- `PersonAlpha`

### 6.3 Optional

- `BuildMode`
  - `PersonOnly`
  - `PersonPlusHeldObject`
  - `DebugOnly`
- `SubjectAlphaMode`

## 7. Core Meaning Rule

`SubjectMask` means the preserved visible subject for a given workflow.

By default:

\[
SubjectMask = PersonMask
\]

When explicit held-object preservation is enabled:

\[
SubjectMask = PersonMask \cup HandheldObjectMask
\]

This means:

- person-only workflows should remain person-only
- prop-inclusive workflows may extend beyond the default person boundary

## 8. Separation Rule

`SubjectMask` must stay conceptually separate from:

- `PersonMask`
- `SkinMask`
- `AccessoryMask`
- `BackgroundMask`

Important meaning split:

- `PersonMask` = default person preservation
- `SubjectMask` = optional extended preservation target

Do not silently rename `PersonMask` to `SubjectMask`.
Do not silently treat them as always identical in every workflow.

## 9. Output Contract

Preferred result structure:

`SubjectMaskBuildResult`

Suggested fields:

- `Status`
- `Confidence`
- `SubjectMask`
- `SubjectAlpha`
- `ExtendedByHeldObject`
- `Warnings`

Minimum required output:

- `SubjectMask`

## 10. Downstream Use Rule

Use `SubjectMask` for:
- optional prop-inclusive extraction
- subject-side preserve routing for special composites
- workflows that explicitly request held-object preservation

Do not use `SubjectMask` to replace:
- `PersonMask` in all default workflows
- `SkinMask`
- clothing or accessory ownership masks

## 11. Debug Outputs

Recommended debug outputs:

- `debug_subject_mask.png`
- `debug_subject_mask_overlay.png`
- `debug_subject_mask_report.json`

Recommended report fields:

- build status
- confidence
- extension mode
- held-object extension used or not used
- warnings

## 12. Do-Not-Do Rules

- Do not change default `PersonMask` behavior by hiding it inside `SubjectMask`.
- Do not force held-object inclusion when no explicit request exists.
- Do not merge background-contact clutter into `SubjectMask`.
- Do not use `SubjectMask` as a shortcut for all non-background content.

## 13. Success Condition

`BuildSubjectMask(...)` is successful when:

- default person-only extraction stays unchanged
- optional held-object extension is explicit and controllable
- person, accessories, clothing, and background meanings remain stable

## 14. One-Line Definition

`BuildSubjectMask(...)` constructs the preserved subject mask from the default `PersonMask`, optionally extending it with `HandheldObjectMask` only when the workflow explicitly requests carried-object preservation.
