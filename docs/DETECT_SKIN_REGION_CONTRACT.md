# DetectSkinRegion Contract

## 1. Purpose

`DetectSkinRegion(...)` defines the first skin-region detector contract for KRetouchPro.

Its role is:
- detect visible skin regions from the original image
- support skin-only retouch decisions
- support jawline, under-jaw, neck, and wrinkle work areas
- prevent clothing, hair, and accessory regions from being treated as skin

This detector is not a beauty filter.
It is a region-definition function.

## 2. Current Stage Definition

At the current project stage:

- eyes, nose, and mouth are already stable enough as anchor-level structure
- the active weakness is lower-face and lower-frame structure
- neck skin, under-jaw skin, and clothing separation matter more than center-face landmark correction

Therefore this detector is defined as:

**a skin-only region detector that supports lower-face, neck, and local retouch safety**

It is not yet:
- a full dermatology classifier
- a blemish corrector
- a wrinkle remover
- a face beautification engine

## 3. Function Name

Preferred name:

`DetectSkinRegion(...)`

Related later functions:

- `BuildSkinMask(...)`
- `BuildFaceSkinMask(...)`
- `BuildDoubleChinMask(...)`
- `BuildNeckWorkArea(...)`
- `ApplySkinRetouch(...)`

## 4. Ownership

Pillar:

- Region Detection

This function detects skin candidates and resolved skin regions.
It must not directly:
- blur the image
- soften wrinkles
- change tone
- replace the background
- classify full clothing structure

## 5. Primary Use Cases

### 5.1 Skin-only retouch protection

Use the detected skin region to ensure:

- skin retouch stays on skin
- clothing does not receive skin smoothing
- hair detail is not treated as skin

### 5.2 Lower-face structure support

Use the detected skin region to support:

- jawline separation
- under-jaw region
- double-chin candidate region
- neck wrinkle candidate region

### 5.3 Mask derivation support

Use this detector as input for:

- `BuildSkinMask(...)`
- `BuildFaceSkinMask(...)`
- `BuildDoubleChinMask(...)`
- `BuildNeckWrinkleMask(...)`

## 6. Input Contract

The detector should accept the following inputs.

### 6.1 Required

- `SourceImage`
  - original-coordinate image buffer
- `ImageWidth`
- `ImageHeight`
- `AnchorSet`
  - should support at least:
    - `left_eye`
    - `right_eye`
    - `nose_tip`
    - `chin`
    - `clothing_start`

### 6.2 Optional but recommended

- `FaceBox`
- `SkinSampleHints`
- `ClothingBoundaryResult`
- `PersonMask`
- `BioMask`
- `HairMask`

### 6.3 Optional mode flag

- `DetectionMode`
  - `FullVisibleSkin`
  - `FaceAndNeckPriority`
  - `UnderJawAndNeckPriority`
  - `DebugOnly`

### 6.4 Average-skin candidate stages

This detector should begin from sampled average skin statistics, not from uncontrolled whole-image skin guessing.

Preferred internal stage order:

```text
SampleSkinColorStatistics(...)
-> BuildAverageSkinCandidateMask(...)
-> SubtractFeatureProtectMasks(...)
-> RefineSkinRegion(...)
-> ResolvedSkinMask
```

Current implementation may reuse:

- `TryComputeSkinStatistics(...)`
- `BuildSkinLikeMask(...)`

Meaning:

- average `Y / Cr / Cb` defines the first stable skin-color reference
- the average-skin candidate mask is the first candidate only
- it must not be treated as the final `SkinMask`
- eyes, nose, lips, and brows should be subtracted before final refinement

Required first protected regions:

- `EyeMask`
- `NoseMask`
- `LipMask`
- `EyebrowMask`

Recommended later protected regions:

- `EyelashMask`
- `FacialHairMask`
- `HairMask`

## 7. Search Region Rule

The detector should not begin with whole-image uncontrolled color picking.

It should begin with anchor-guided search zones.

### 7.1 Initial search zones

Preferred first-pass search zones:

- face center skin zone
- cheek and lower-face skin zone
- under-jaw band
- front neck band above clothing boundary

### 7.2 Region expansion rule

After seed detection, the region may expand only while remaining consistent with:

- skin color / texture evidence
- anatomical continuity
- exclusion masks for hair, eyebrows, beard, clothing, and accessories

## 8. Detection Evidence

The detector should use multiple weak cues together.

Preferred evidence groups:

### 8.1 Color evidence

- skin-like hue range
- moderate local color continuity
- support for different exposure and white-balance conditions

### 8.2 Texture evidence

- skin-like softness and pore-scale texture
- avoidance of fabric weave, hair strands, and hard accessory edges

### 8.3 Structural evidence

- anchor proximity to face and neck geometry
- continuity from face to chin to neck
- plausible under-jaw and neck shape

### 8.4 Exclusion evidence

Reject or reduce confidence where the region behaves more like:

- hair
- eyebrow
- eyelash
- beard
- clothing
- glasses or accessories

## 9. Output Contract

The detector should return a structured result.

Preferred result shape:

```text
SkinRegionDetectionResult
```

Required fields:

- `Status`
  - `Detected`
  - `WeakCandidate`
  - `PartialDetected`
  - `Failed`
- `Confidence`
  - normalized `0.0 .. 1.0`
- `SkinCandidateMask`
- `ResolvedSkinMask`
- `FaceSkinMask`
- `NeckSkinMask`
- `Warnings`

Optional fields:

- `UnderJawSkinMask`
- `DebugSkinSeedMask`
- `DebugColorEvidenceMask`
- `DebugTextureEvidenceMask`
- `DebugSearchRegion`

### 9.1 Original average-color model translation

The old average-face-color workflow maps into the current detector as follows:

- `AverageFaceColorMaskBuilder.Build(...)`
  -> `BuildAverageSkinCandidateMask(...)`
- `CreateDefaultSkinColorReferences(...)`
  -> `GetDefaultSkinSampleHints(...)`
- `manualReferenceColors`
  -> `SkinSampleHints`
- `BuildSkinRangeMask(...)`
  -> `BuildAverageSkinCandidateMask(...)`
- `CaptureSkinMaskRange()`
  -> `SkinColorRangeTolerance`

Important:

- old `average color mask` is not the final `SkinMask`
- it is the first color-evidence candidate that must pass through feature subtraction and refinement

## 10. Fallback Rule

This detector must fail safely.

If stable skin detection is not available:

- do not classify clothing as skin
- do not force a large skin fill
- do not overwrite stable anchors

Fallback behavior:

1. keep face-center anchor structure unchanged
2. allow partial result if face skin is stable but neck skin is weak
3. return warning state
4. prefer smaller safe skin regions over larger unsafe skin regions

## 11. Non-Goals

This function must not try to solve all skin-related processing.

Non-goals for this contract:

- acne detection
- wrinkle scoring
- redness correction
- skin smoothing
- tone evening
- scar classification
- tattoo classification

Those belong to later feature-specific or retouch-specific stages.

## 12. Dependencies

This function should depend on:

- stable anchor layer
- original-coordinate source image
- optional clothing / hair guidance when available

This function should not depend on:

- final background replacement
- final texture retouch
- final export rendering

## 13. Dictionary Alignment

This detector must preserve the existing mask meaning separation:

- `SkinMask` is for skin retouch
- `PersonMask` is for subject preservation and background replacement
- `ClothingMask` is not part of skin
- `HairMask` is not part of skin

Important rules:

- neck is not face
- hair is not face skin
- beard is not skin target by default
- exposed skin may exist outside the face region

## 14. Debug Output Rule

The first implementation must be debug-friendly.

Preferred debug outputs:

- skin seed overlay
- resolved skin overlay
- under-jaw skin mask
- neck skin mask
- exclusion mask overlay
- confidence and warning text

Recommended debug filenames:

- `debug_skin_region_seed_mask.png`
- `debug_skin_region_overlay.png`
- `debug_skin_region_neck_mask.png`
- `debug_skin_region_under_jaw_mask.png`
- `debug_skin_region_report.json`

## 15. Relation To Other Detectors

Current intended relation:

```text
anchors
-> DetectClothingBoundary(...)
-> DetectSkinRegion(...)
-> BuildSkinMask(...)
-> BuildDoubleChinMask(...) / BuildNeckWrinkleMask(...)
```

Meaning:

- clothing boundary helps define where neck skin should stop
- skin region helps define where neck and under-jaw work can safely occur
- later feature masks derive from skin, not from generic person area

## 16. First Implementation Scope

The first implementation should stay small.

Recommended first-pass scope:

1. anchor-guided face + under-jaw + neck skin detection
2. clothing exclusion support when boundary guidance exists
3. safe partial result behavior
4. debug overlay export

Do not start with:

- full-body skin parsing
- blemish detection
- wrinkle scoring
- cosmetic color adjustment

## 17. Success Condition

The detector is successful when:

- clothing is no longer mistaken for skin in the lower portrait area
- under-jaw and neck work areas become easier to define safely
- later skin retouch can stay inside real visible skin regions
- weak cases fail small rather than leaking into fabric or hair

## 18. One-Line Definition

`DetectSkinRegion(...)` detects safe visible skin regions from anchor-guided original-image evidence, especially for face, under-jaw, and neck support, without confusing skin with clothing, hair, or accessory regions.
