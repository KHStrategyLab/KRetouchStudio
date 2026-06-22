# BuildSkinMask Contract

## 1. Purpose

`BuildSkinMask(...)` defines the first stable skin-mask construction contract for KRetouchPro.

Its role is:
- convert skin-region detection output into a reusable production mask
- preserve the meaning of skin-only editing
- provide a safe mask for skin retouch, jawline support, neck work, and later feature masks

This function does not detect skin from scratch.
It resolves and stabilizes skin meaning from existing evidence.

## 2. Current Stage Definition

At the current project stage:

- `DetectSkinRegion(...)` is the detector-side entry for visible skin evidence
- `DetectClothingBoundary(...)` helps define where neck skin should stop
- lower-face, neck, jawline, and crop support are the active priorities

Therefore `BuildSkinMask(...)` is defined as:

**the mask-construction step that turns skin-region evidence into a safe reusable SkinMask**

It is not:
- a person mask builder
- a clothing mask builder
- a skin beautification filter
- a wrinkle or blemish corrector

## 3. Function Name

Preferred name:

`BuildSkinMask(...)`

Related later functions:

- `BuildFaceSkinMask(...)`
- `BuildSkinSafeMask(...)`
- `BuildDoubleChinMask(...)`
- `BuildNeckWrinkleMask(...)`
- `ApplySkinRetouch(...)`

## 4. Ownership

Pillar:

- Mask Construction

This function should construct masks.
It must not directly:
- detect the visible face from scratch
- smooth pixels
- warp geometry
- replace background
- classify garments

## 5. Primary Use Cases

### 5.1 Skin retouch routing

Use `SkinMask` to define where skin-only retouch may run.

### 5.2 Lower-face work support

Use derived skin masks for:

- under-jaw support
- neck skin support
- jawline separation support
- double-chin and neck-wrinkle work

### 5.3 Feature-mask derivation

Use `SkinMask` as the parent base for:

- `DoubleChinMask`
- `NeckWrinkleMask`
- future spot / wrinkle / scar / mole masks

## 6. Input Contract

The mask builder should accept the following inputs.

### 6.1 Required

- `SkinRegionDetectionResult`

### 6.2 Recommended

- `ClothingBoundaryDetectionResult`
- `PersonMask`
- `BioMask`
- `FaceRegionMask`
- `HairMask`
- `EyebrowMask`
- `EyelashMask`
- `FacialHairMask`
- `ClothingMask`
- `AccessoryMask`

### 6.3 Optional

- `AnchorSet`
- `NeckGuide`
- `BuildMode`
  - `SkinRetouch`
  - `LowerFaceSupport`
  - `DebugOnly`

## 7. Core Meaning Rule

`SkinMask` means visible skin only.

It must include:
- face skin
- ear skin when visible
- neck skin when visible
- any exposed skin that is intentionally in scope

It must exclude:
- scalp hair
- eyebrow
- eyelash
- beard / moustache / chin hair
- clothing
- accessory
- background

## 8. Construction Rule

`BuildSkinMask(...)` should not simply return the detector candidate unchanged.

It should:

1. start from resolved skin-region evidence
2. keep anchor-consistent face and neck continuity
3. subtract exclusion masks
4. split or preserve useful subregions
5. return a stable reusable mask set

### 8.1 Upstream ownership boundary

This builder does not own the first average-color detection step.

It starts after the detector-side stages below:

```text
SampleSkinColorStatistics(...)
-> BuildAverageSkinCandidateMask(...)
-> SubtractFeatureProtectMasks(...)
-> RefineSkinRegion(...)
-> ResolvedSkinMask
```

That means:

- average `Y / Cr / Cb` sampling does not belong to this builder
- average-skin candidate generation does not belong to this builder
- eye / nose / lip / eyebrow subtraction should already have happened inside `DetectSkinRegion(...)`
- this builder owns stabilization and reusable mask construction after `ResolvedSkinMask`

## 9. Preferred Output Structure

Preferred result shape:

```text
SkinMaskBuildResult
```

Required fields:

- `Status`
  - `Built`
  - `PartialBuilt`
  - `FallbackBuilt`
  - `Failed`
- `Confidence`
  - normalized `0.0 .. 1.0`
- `SkinMask`
- `FaceSkinMask`
- `NeckSkinMask`
- `Warnings`

Optional fields:

- `UnderJawSkinMask`
- `SkinSafeMask`
- `SkinRetouchMask`
- `DebugBeforeExclusionMask`
- `DebugAfterExclusionMask`
- `DebugSubregionOverlay`

## 10. Subregion Rule

The builder should preserve meaningful subregions where possible.

Preferred first subregions:

- `FaceSkinMask`
- `NeckSkinMask`
- `UnderJawSkinMask`

Reason:

These subregions are directly useful for:
- jawline support
- double-chin support
- neck-wrinkle support
- skin retouch protection

## 11. Exclusion Rule

The builder must support hard exclusion of non-skin regions.

Preferred exclusions:

- `HairMask`
- `EyebrowMask`
- `EyelashMask`
- `FacialHairMask`
- `ClothingMask`
- `AccessoryMask`

Preferred conceptual rule:

```text
SkinSafeMask = SkinMask
             * (1 - HairMask)
             * (1 - EyebrowMask)
             * (1 - EyelashMask)
             * (1 - FacialHairMask)
             * (1 - ClothingMask)
             * (1 - AccessoryMask)
```

The exact implementation may vary, but the meaning must remain stable.

## 12. Fallback Rule

This builder must fail safely.

If detector evidence is weak:

- do not expand skin into clothing
- do not expand skin into hair
- do not prefer a large uncertain skin mask over a small safe one

Fallback behavior:

1. keep stable face skin if available
2. allow neck skin to drop out if uncertain
3. return `PartialBuilt` or `FallbackBuilt`
4. expose warnings for downstream diagnostics

## 13. Downstream Consumers

This mask builder is expected to support:

- `ApplySkinRetouch(...)`
- `BuildDoubleChinMask(...)`
- `BuildNeckWrinkleMask(...)`
- `BuildJawlineMask(...)`
- `BuildClothingBoundaryWorkArea(...)`
- `BlendBackground(...)` only indirectly through safe region separation

## 14. Non-Goals

This function must not try to solve:

- full person matte construction
- garment segmentation
- hair segmentation
- beauty filter strength
- blemish classification
- wrinkle scoring

Those belong to other pillars.

## 15. Debug Output Rule

The first implementation must be debug-friendly.

Preferred debug outputs:

- resolved skin candidate overlay
- exclusion-before / exclusion-after overlays
- face skin mask
- neck skin mask
- under-jaw skin mask
- warning / confidence report

Recommended debug filenames:

- `debug_skin_mask_before_exclusion.png`
- `debug_skin_mask_after_exclusion.png`
- `debug_face_skin_mask.png`
- `debug_neck_skin_mask.png`
- `debug_under_jaw_skin_mask.png`
- `debug_skin_mask_report.json`

## 16. Relationship To Existing Definitions

This builder must preserve the project-wide distinction:

- `PersonMask` = subject preservation / background replacement
- `SkinMask` = skin-only operations
- `ClothingMask` = clothing-only operations
- `HairMask` = hair-only operations

`SkinMask` must never quietly degrade into `PersonMask`.

This also means:

- `average skin candidate mask` is not `SkinMask`
- `ResolvedSkinMask` is upstream detector output
- `SkinMask` is the stabilized reusable production mask built from that resolved output

## 17. First Implementation Scope

The first implementation should stay small.

Recommended first-pass scope:

1. accept resolved skin-region evidence
2. subtract known exclusion masks
3. produce `SkinMask`, `FaceSkinMask`, and `NeckSkinMask`
4. support `PartialBuilt`
5. export debug masks

Do not start with:

- full body skin parsing
- cosmetic scoring
- feature classification inside the mask builder

## 18. Success Condition

`BuildSkinMask(...)` is successful when:

- skin retouch can run without leaking into clothing or hair
- neck and under-jaw work areas become safer to define
- lower-face work becomes more stable than raw detector output alone
- weak cases degrade safely

## 19. One-Line Definition

`BuildSkinMask(...)` converts anchor-guided skin-region evidence into a safe reusable skin-only mask set for retouch and lower-face support, while explicitly excluding clothing, hair, and accessory regions.
