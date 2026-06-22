# BuildDoubleChinMask Contract

## 1. Purpose

`BuildDoubleChinMask(...)` defines the first double-chin mask construction contract for KRetouchPro.

Its role is:
- isolate the under-jaw fullness region
- keep double-chin work separate from jawline and general neck skin
- provide a dedicated work mask for local retouch or later shape support

This function does not define all lower-face skin.
It defines the double-chin target only.

## 2. Current Stage Definition

At the current project stage:

- `chin` is already an active lower-face anchor
- `clothing_start` is already an active lower framing guide
- `DetectClothingBoundary(...)`, `DetectSkinRegion(...)`, and `BuildSkinMask(...)` are the intended prerequisites

Therefore `BuildDoubleChinMask(...)` is defined as:

**a derived lower-face feature mask built from skin, neck, and jawline support**

It is not:
- the whole neck mask
- the whole jawline mask
- the whole lower-face warp area

## 3. Function Name

Preferred name:

`BuildDoubleChinMask(...)`

Related later functions:

- `BuildJawlineMask(...)`
- `BuildNeckWrinkleMask(...)`
- `BuildDoubleChinWorkArea(...)`
- `ApplyDoubleChinRetouch(...)`

## 4. Ownership

Pillar:

- Mask Construction

This function should build a feature mask.
It must not directly:
- perform warp
- perform liquify
- smooth pixels
- redefine person or clothing masks

## 5. Input Contract

The builder should accept the following inputs.

### 5.1 Required

- `SkinMaskBuildResult`
- `AnchorSet`
  - at minimum:
    - `left_eye`
    - `right_eye`
    - `chin`
    - `clothing_start`

### 5.2 Recommended

- `ClothingBoundaryDetectionResult`
- `JawlineMask`
- `NeckGuide`
- `FaceRegionMask`
- `NeckSkinMask`
- `UnderJawSkinMask`

## 6. Core Meaning Rule

`DoubleChinMask` means:

- visible under-jaw fullness
- between jawline and upper neck
- still above the lower clothing boundary

It must remain separate from:

- `JawlineMask`
- `NeckWrinkleMask`
- full `NeckSkinMask`

## 7. Construction Rule

Preferred conceptual rule:

```text
DoubleChinMask
= UnderJawCandidate
  ∩ NeckSkinMask
  ∩ Below(JawLine)
  ∩ Above(NeckBaseLine)
  * DoubleChinProb
```

Meaning:

- it lives below the jawline
- it remains inside valid skin support
- it stops before lower neck / clothing takeover

## 8. Output Contract

Preferred result shape:

```text
DoubleChinMaskBuildResult
```

Required fields:

- `Status`
  - `Built`
  - `PartialBuilt`
  - `FallbackBuilt`
  - `Failed`
- `Confidence`
- `DoubleChinMask`
- `DoubleChinWorkMask`
- `Warnings`

Optional fields:

- `UnderJawCandidateMask`
- `JawlineBelowBandMask`
- `DebugDoubleChinBandMask`

## 9. Fallback Rule

If clear double-chin evidence is weak:

- do not expand into full neck skin
- do not expand into clothing
- do not force a large lower-face mask

Fallback behavior:

1. keep the result small
2. stay near under-jaw support
3. return partial or fallback status

## 10. First Implementation Scope

Recommended first pass:

1. consume `SkinMaskBuildResult`
2. consume anchor-guided lower-face geometry
3. isolate a safe under-jaw band
4. exclude clothing boundary spill
5. export debug mask

## 11. Success Condition

The mask is successful when:

- it isolates the under-jaw target better than full neck skin
- it does not leak into clothing
- it remains distinct from general jawline support

## 12. One-Line Definition

`BuildDoubleChinMask(...)` constructs a dedicated under-jaw feature mask from anchor-guided skin and lower-face support, keeping double-chin work separate from both jawline and general neck regions.
