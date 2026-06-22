# DetectClothingBoundary Contract

## 1. Purpose

`DetectClothingBoundary(...)` is the first lower-frame detector contract for KRetouchPro.

Its role is not full garment understanding.

Its immediate role is:
- refine the neck-to-clothing boundary below the face
- support `clothing_start` guide stabilization
- support lower crop stability
- support future neck, jawline, double-chin, and background-replacement work areas

## 2. Current Stage Definition

At the current project stage:

- eyes, nose, and mouth are already stable enough
- lower-face and lower-frame guidance are the active weak area
- `clothing_start` already exists as a manual guide anchor

Therefore this detector is defined as:

**a lower portrait boundary detector centered on the neck-to-clothing transition**

It is not yet:
- a full clothing segmentation engine
- a garment classifier
- a fashion attribute detector

## 3. Function Name

Preferred name:

`DetectClothingBoundary(...)`

Related later functions:

- `BuildClothingMask(...)`
- `BuildClothingBoundaryWorkArea(...)`
- `BuildPortraitTrainingCropBox(...)`

## 4. Ownership

Pillar:

- Region Detection

This function should detect a boundary candidate.
It must not directly:
- warp the image
- apply retouch
- replace the background
- finalize a clothing mask for all clothing regions

## 5. Primary Use Cases

### 5.1 Portrait training crop stabilization

Use the detected lower clothing boundary to improve the bottom decision for:

- `BuildPortraitTrainingCropBox(...)`

### 5.2 Lower-face work-area support

Use the detected boundary to support:

- neck work area
- double-chin work area
- jawline-to-neck separation

### 5.3 Clothing-preserve support

Use the detected boundary later as a seed for:

- clothing preservation masks
- subject-matte lower-body stability

## 6. Input Contract

The detector should accept the following inputs.

### 6.1 Required

- `SourceImage`
  - original-coordinate image buffer
- `ImageWidth`
- `ImageHeight`
- `AnchorSet`
  - must support at least:
    - `left_eye`
    - `right_eye`
    - `chin`
    - `clothing_start`

### 6.2 Optional but recommended

- `FaceBox`
- `PortraitTrainingCropCandidate`
- `SkinMask`
- `BioMask`
- `PersonMask`

### 6.3 Optional mode flag

- `DetectionMode`
  - `GuideAssist`
  - `MaskAssist`
  - `DebugOnly`

## 7. Search Region Rule

The detector must not scan the whole image first.

The first search region should be local and anatomy-guided.

### 7.1 Initial search band

Preferred initial search band:

- horizontal center based on eye center and chin center
- vertical start near `chin`
- vertical continuation downward through `clothing_start`

### 7.2 Expected region

The detector should focus on:

- under-chin region
- front neck band
- neckline / collar / upper garment edge

It should not start from:

- sleeves
- torso-wide clothing segmentation
- lower-body clothing

## 8. Detection Evidence

The detector should combine lightweight evidence, not a single hard rule.

Preferred evidence groups:

### 8.1 Edge evidence

- strong horizontal or curved boundary under neck skin
- neckline edge
- collar edge

### 8.2 Tone / color transition evidence

- skin-to-fabric color change
- neck shadow to clothing tone separation

### 8.3 Region consistency evidence

- downward region becomes less skin-like
- boundary stays connected across expected neck width

### 8.4 Anchor proximity evidence

- candidate boundary should remain anatomically plausible relative to:
  - `chin`
  - `clothing_start`
  - neck width implied by lower-face geometry

## 9. Output Contract

The detector should return a structured result.

Preferred result shape:

```text
ClothingBoundaryDetectionResult
```

Required fields:

- `Status`
  - `Detected`
  - `WeakCandidate`
  - `GuideOnlyFallback`
  - `Failed`
- `Confidence`
  - normalized `0.0 .. 1.0`
- `BoundaryPolyline`
  - local or image-coordinate ordered boundary points
- `BoundaryBandMask`
  - thin band around the detected boundary
- `ClothingStartGuideY`
  - resolved lower guide Y
- `Warnings`
  - zero or more detector warnings

Optional fields:

- `UpperClothingCandidateMask`
- `NeckBelowBoundaryMask`
- `DebugEdgeMask`
- `DebugColorTransitionMask`
- `DebugSearchRegion`

## 10. Fallback Rule

This detector must fail safely.

If a reliable clothing boundary is not found:

- do not hallucinate a strong boundary
- do not overwrite stable face anchors
- do not invent a full clothing mask

Fallback behavior:

1. keep `clothing_start` manual guide
2. return `GuideOnlyFallback`
3. expose warning state for debug and later review

## 11. Non-Goals

This function must not try to solve all of clothing analysis.

Non-goals for this contract:

- garment type classification
- sleeve detection
- full torso segmentation
- fashion parsing
- accessory parsing
- full person matte construction

Those belong to later detector or mask stages.

## 12. Dependencies

This function should depend on:

- stable anchor layer
- image evidence in original coordinate space

This function should not depend on:

- final background replacement
- final skin retouch
- mesh warp result

## 13. Debug Output Rule

The first implementation must be debug-friendly.

Preferred debug outputs:

- search region overlay
- detected boundary polyline overlay
- boundary band mask
- guide-vs-detected comparison overlay
- confidence and warning text

Recommended debug filenames:

- `debug_clothing_boundary_search_region.png`
- `debug_clothing_boundary_overlay.png`
- `debug_clothing_boundary_band_mask.png`
- `debug_clothing_boundary_report.json`

## 14. Relation To Current Manual Guide

Current state:

- `clothing_start` is a manual guide anchor

This detector should evolve that state, not replace it abruptly.

Current intended relation:

```text
manual clothing_start
-> local clothing-boundary detection
-> resolved lower boundary guide
-> crop / neck / jawline support
```

That means:

- manual guide remains valid
- detector refines around it
- detector failure falls back to guide

## 15. Downstream Consumers

This detector is expected to support:

- `BuildPortraitTrainingCropBox(...)`
- `BuildClothingBoundaryWorkArea(...)`
- `BuildClothingMask(...)`
- `BuildDoubleChinWorkArea(...)`
- `BuildNeckWorkArea(...)`
- later `BuildSubjectMatte(...)`

## 16. First Implementation Scope

The first implementation should stay small.

Recommended first-pass scope:

1. local search region under chin
2. neck-to-clothing boundary candidate scan
3. confidence score
4. fallback to manual guide
5. debug overlay export

Do not start with:

- full clothing mask
- attribute classification
- background interaction
- hair interaction

## 17. Success Condition

The detector is successful when:

- lower crop becomes more stable than guide-only mode
- jawline / neck work areas are easier to separate
- failure cases fall back cleanly without damaging face-center anchors

## 18. One-Line Definition

`DetectClothingBoundary(...)` detects the local neck-to-clothing transition below the face, using anchors plus image evidence, and returns a safe lower-boundary result that can refine `clothing_start` without forcing a false full-clothing interpretation.
