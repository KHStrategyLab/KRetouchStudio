# Clothing Boundary -> Skin Region -> Skin Mask Flow

## 1. Purpose

This document defines the first stable lower-frame processing flow for KRetouchPro.

It connects three contracts:

1. `DetectClothingBoundary(...)`
2. `DetectSkinRegion(...)`
3. `BuildSkinMask(...)`

This flow is the first intended bridge from:

- manual guide anchors
- lower-face and neck structure
- reusable skin-safe masks

## 2. Why This Flow Comes First

At the current project stage:

- eyes, nose, and mouth are already stable enough
- the active instability is below the face center
- crop stability, neck separation, jawline support, and under-jaw work all depend on lower boundary clarity

That makes this chain the most practical next structural step.

## 3. Flow Summary

```text
AnchorSet
-> DetectClothingBoundary(...)
-> DetectSkinRegion(...)
-> BuildSkinMask(...)
-> BuildDoubleChinMask(...) / BuildNeckWrinkleMask(...) / ApplySkinRetouch(...)
```

## 4. Step 1 - Anchor Intake

Required anchor minimum:

- `left_eye`
- `right_eye`
- `nose_tip`
- `chin`
- `clothing_start`

Role:

- define center face
- define lower-face direction
- define local search zone under chin
- provide fallback guide behavior

## 5. Step 2 - DetectClothingBoundary(...)

Input focus:

- under-chin band
- front neck band
- neckline / collar candidate area
- current `clothing_start` guide

Output focus:

- boundary polyline
- boundary band
- resolved lower guide Y
- confidence / warnings

Failure behavior:

- use guide fallback
- do not invent full clothing segmentation

## 6. Step 3 - DetectSkinRegion(...)

This step consumes:

- original image
- anchors
- clothing boundary guidance when available

Main role:

- detect visible face / under-jaw / neck skin
- avoid treating clothing and hair as skin

Important relationship:

- clothing boundary helps define where neck skin should stop
- skin continuity helps define where lower-face support can safely continue

Failure behavior:

- allow partial result
- prefer smaller safe skin region over larger unsafe fill

## 7. Step 4 - BuildSkinMask(...)

This step consumes:

- skin-region detection result
- optional exclusion masks
- optional clothing and hair guidance

Main role:

- convert detector evidence into reusable skin-only masks
- subtract non-skin structures
- return stable skin subregions

Expected outputs:

- `SkinMask`
- `FaceSkinMask`
- `NeckSkinMask`
- optional `UnderJawSkinMask`

## 8. Immediate Downstream Uses

The first downstream consumers should be:

### 8.1 Crop support

- lower crop stabilization
- neck / lower-frame stability review

### 8.2 Work-area support

- `BuildDoubleChinWorkArea(...)`
- `BuildNeckWorkArea(...)`

### 8.3 Later feature masks

- `BuildDoubleChinMask(...)`
- `BuildNeckWrinkleMask(...)`

### 8.4 Retouch safety

- `ApplySkinRetouch(...)`

## 9. Data Ownership By Stage

### 9.1 DetectClothingBoundary(...)

Owns:

- lower boundary candidate
- neckline transition evidence

Does not own:

- final skin mask
- final clothing mask

### 9.2 DetectSkinRegion(...)

Owns:

- visible skin evidence
- face / neck skin candidates

Does not own:

- final reusable skin-safe mask set

### 9.3 BuildSkinMask(...)

Owns:

- final reusable skin-only mask meaning
- exclusion-safe skin region output

Does not own:

- retouch pixels
- warp logic

## 10. Safety Rules

This chain must follow these rules:

1. Do not let clothing become skin.
2. Do not let hair become skin.
3. Do not let detector weakness force a large fake region.
4. Prefer fallback and partial status over unsafe fill.
5. Preserve center-face anchors even if lower detection is weak.

## 11. Debug Rules

The first implementation of this chain should export debug data per stage.

Recommended sequence outputs:

- `debug_clothing_boundary_overlay.png`
- `debug_skin_region_overlay.png`
- `debug_skin_mask_after_exclusion.png`
- `debug_lower_frame_chain_report.json`

Recommended report fields:

- clothing boundary status
- clothing boundary confidence
- skin region status
- skin region confidence
- skin mask build status
- skin mask confidence
- fallback flags
- warnings

## 12. First Implementation Scope

The first implementation should remain narrow.

Recommended first pass:

1. local clothing boundary detection around manual guide
2. under-jaw and neck skin detection
3. reusable skin mask build
4. debug overlay export

Do not start with:

- full clothing segmentation
- full body skin parsing
- feature retouch automation
- hairline solving in the same pass

## 13. Expected Benefit

If this chain works, the project gains:

- better lower crop stability
- safer neck and under-jaw work areas
- cleaner separation between skin and clothing
- a usable parent mask for later double-chin and neck-wrinkle logic

## 14. One-Line Definition

This flow turns lower-face anchors plus local image evidence into a stable neck-aware skin mask chain that can safely support crop logic, jawline support, and later local retouch work.
