# Function Pillars Catalog

## 1. Purpose

This document defines the first stable function catalog for KRetouchPro.

It combines:
- original named function directions found in the original project documents
- current dictionary terms already defined for photographic meaning and body/hair/clothing locations
- the current anchor / region / crop / retouch layering direction

This is not a final implementation map.
It is the naming and ownership scaffold that future implementation should follow.

Base domain note:
- every mask in the project is defined inside the selected working-image domain `1`
- `1` means the full pixel domain of the single current working image

## 2. Naming Rule

Function names should express photographic or anatomical intent first.

Preferred style:
- `Detect...`
- `Build...Mask`
- `Build...WorkMask`
- `Build...CropBox`
- `Estimate...`
- `Protect...`
- `Warp...`
- `Blend...`
- `Apply...`

Avoid:
- unnamed math-only helpers as product entry points
- mixed-purpose functions that detect, warp, blend, and filter in one step

## 3. Ownership Rule

The system should be divided into stable pillars.

Each pillar owns one kind of responsibility:

1. Analysis and anchor intake
2. Region detection
3. Mask construction
4. Crop and work-area construction
5. Geometry warp
6. Background / matte compositing
7. Tone and texture retouch
8. Preview / orchestration

## 4. Pillar 1 - Analysis And Anchor Intake

Purpose:
Read the image, detect the visible subject structure, and prepare anchor-level guidance.

### 4.1 Original named directions

- `DetectImageContent(...)`
- `DetectHeadRegion(...)`
- `EstimateHeadPivot(...)`
- `DetectLips(...)`
- `DetectPupils(...)`

### 4.2 Current dictionary-linked terms

- `Face`
- `FaceRegion`
- `Neck`
- `Jawline`
- `DoubleChin`
- `Hairline`
- `Neckline`

### 4.3 First scaffold functions

- `DetectImageContent(...)`
- `DetectVisiblePerson(...)`
- `DetectBiologicalBody(...)`
- `DetectFaceBox(...)`
- `DetectFaceLandmarks(...)`
- `DetectBodyLandmarks(...)`
- `EstimateClothingStartGuide(...)`
- `EstimateJawAnchorGuide(...)`

### 4.4 Current practical note

Eyes, nose, and mouth are already strong enough to stay mostly detector-driven.
Current manual refinement pressure is lower-face and lower-framing guidance.

## 5. Pillar 2 - Region Detection

Purpose:
Build detector-level candidate regions from anchors, image cues, and anatomical definitions.

### 5.1 Original named directions

- `DetectVisiblePerson(...)`
- `DetectBiologicalBody(...)`
- `DetectClothing(...)`
- `DetectWornAccessories(...)`
- `DetectAcneSpots(...)`
- `DetectWrinkles(...)`

### 5.2 Dictionary-linked region targets

- `FaceSkin`
- `Neck`
- `FrontNeck`
- `NeckWrinkle`
- `ScalpHair`
- `Hairline`
- `BabyHair`
- `Clothing`
- `Neckline`
- `Jawline`
- `DoubleChin`

### 5.3 First scaffold detector functions

- `DetectSkinRegion(...)`
- `DetectNeckRegion(...)`
- `DetectClothingBoundary(...)`
- `DetectHairRegion(...)`
- `DetectHairlineRegion(...)`
- `DetectJawlineRegion(...)`
- `DetectDoubleChinRegion(...)`
- `DetectNeckWrinkleRegion(...)`

### 5.4 Immediate priority

1. `DetectClothingBoundary(...)`
2. `DetectSkinRegion(...)` for under-jaw and neck
3. `DetectHairRegion(...)`

## 6. Pillar 3 - Mask Construction

Purpose:
Turn candidate detections into stable reusable masks.

### 6.1 Original named directions

- `BuildSubjectMatte(...)`
- `FillSmallInternalHoles(...)`

### 6.2 Formula-spec mask terms

- `PersonMask`
- `BioMask`
- `SkinMask`
- `ClothingMask`
- `AccessoryMask`
- `SubjectMask`
- `HeadCarrierMask`
- `HairMask`
- `FaceSkinMask`
- `JawlineMask`
- `DoubleChinMask`
- `HairColorTargetMask`

### 6.3 First scaffold mask functions

- `BuildPersonMask(...)`
- `BuildBioMask(...)`
- `BuildSkinMask(...)`
- `BuildClothingMask(...)`
- `BuildAccessoryMask(...)`
- `BuildSubjectMask(...)`
- `BuildHeadCarrierMask(...)`
- `BuildHairMask(...)`
- `BuildFaceSkinMask(...)`
- `BuildJawlineMask(...)`
- `BuildDoubleChinMask(...)`
- `BuildNeckWrinkleMask(...)`
- `BuildHairColorTargetMask(...)`
- `FillSmallInternalHoles(...)`

### 6.4 Mask policy note

The project already has a strong dictionary distinction:
- `PersonMask` is for person preservation and background replacement
- `SkinMask` is for skin retouch only
- `ClothingMask` is for clothing preservation and clothing-only edits

These meanings must not collapse into one generic mask.

## 7. Pillar 4 - Crop And Work-Area Construction

Purpose:
Build framing boxes and task-specific local work regions.

### 7.1 Current named functions already in the current project

- `BuildPortraitTrainingCropBox(...)`
- `BuildExportFaceBoxFromManualAnchors(...)`

### 7.2 Dictionary-linked framing and location terms

- `PortraitCrop`
- `HeadCrop`
- `HeadAndShouldersCrop`
- `HeadAndChestCrop`
- `Neck`
- `Neckline`
- `ClothingStartGuide`

### 7.3 First scaffold functions

- `BuildPortraitTrainingCropBox(...)`
- `BuildHeadAndShouldersCropBox(...)`
- `BuildFaceWorkArea(...)`
- `BuildNoseWorkArea(...)`
- `BuildJawlineWorkArea(...)`
- `BuildDoubleChinWorkArea(...)`
- `BuildNeckWorkArea(...)`
- `BuildBackgroundReplaceWorkArea(...)`
- `BuildClothingBoundaryWorkArea(...)`

### 7.4 Current practical note

The current lower crop is stabilized by:
- eye center
- `chin`
- `clothing_start`

At this stage, `clothing_start` is a guide anchor, not a full detector result.

## 8. Pillar 5 - Geometry Warp

Purpose:
Move structure before any skin beautification or background compositing.

### 8.1 Original named directions

- `WarpHeadCarrier(...)`
- `WarpHeadTilt(...)`
- `WarpShapeBalanceMap(...)`
- `WarpFaceShapeControlsCompatibility(...)`
- `WarpLandmarks(...)`
- `ProtectBodyAndClothing(...)`

### 8.2 First scaffold functions

- `WarpHeadCarrier(...)`
- `WarpJawline(...)`
- `WarpFaceBalanceMap(...)`
- `ProtectBodyAndClothing(...)`
- `ProtectHairDetail(...)`
- `ProtectShoulderLine(...)`
- `ReconnectNeckSeam(...)`

### 8.3 Ownership note

Warp functions should consume already-defined anchors, masks, and work areas.
They should not re-decide mask meaning internally.

## 9. Pillar 6 - Background And Matte Compositing

Purpose:
Replace or blend background while preserving the subject boundary.

### 9.1 Original named directions

- `BuildSubjectMatte(...)`
- `DetectExposedBackgroundGap(...)`
- `FillExposedBackground(...)`
- `BlendBackground(...)`
- `BlendLayerIntoBase(...)`

### 9.2 First scaffold functions

- `BuildSubjectMatte(...)`
- `BuildBackgroundMask(...)`
- `DetectExposedBackgroundGap(...)`
- `FillExposedBackground(...)`
- `BlendBackground(...)`
- `BlendLayerIntoBase(...)`
- `FeatherMaskEdge(...)`
- `MatchEdgeTone(...)`
- `HealSeam(...)`
- `StabilizeBlendResult(...)`

## 10. Pillar 7 - Tone And Texture Retouch

Purpose:
Apply color, exposure, texture, and local cleanup after structure is stable.

### 10.1 Original named directions

- `ApplyToneResponse(...)`
- `ApplyTextureRetouch(...)`
- `CorrectAcneSpots(...)`
- `SoftenWrinkles(...)`
- `AdjustLipTone(...)`
- `BalanceEyeBrightness(...)`

### 10.2 First scaffold functions

- `ApplyToneResponse(...)`
- `ApplyTextureRetouch(...)`
- `ApplySkinRetouch(...)`
- `CorrectAcneSpots(...)`
- `SoftenWrinkles(...)`
- `AdjustLipTone(...)`
- `BalanceEyeBrightness(...)`
- `ApplyHairColor(...)`
- `ApplyClothingTone(...)`

### 10.3 Ownership note

This pillar should use:
- `ApplyMask`
- `ProtectMask`
- `BlockMask`

It should not invent region meaning on the fly.

## 11. Pillar 8 - Preview And Orchestration

Purpose:
Route the current request through the right operations without burying behavior inside scattered formulas.

### 11.1 Original named directions

- `RenderPreviewCore(...)`
- `ApplyPhotoAdjustmentsAsync(...)`
- `ApplyShapeBalanceOnlyPreviewAsync(...)`
- `ApplyDummyMaskRetouchAsync(...)`
- `ApplyToneAndTextureCheckout(...)`

### 11.2 First scaffold functions

- `RenderPreviewCore(...)`
- `RenderExportCore(...)`
- `ApplyPhotoAdjustmentsAsync(...)`
- `ApplyShapeBalanceOnlyPreviewAsync(...)`
- `ApplyDummyMaskRetouchAsync(...)`
- `ApplyToneAndTextureCheckout(...)`

### 11.3 Orchestration rule

Preview orchestration should call named operations such as:
- `DetectHeadRegion(...)`
- `BuildSubjectMatte(...)`
- `WarpHeadCarrier(...)`
- `BlendBackground(...)`
- `ApplyToneResponse(...)`
- `ApplyTextureRetouch(...)`

It should not become the home of anonymous geometry or pixel policy logic.

## 12. Immediate Core Set

The smallest useful foundation for the next stage is:

### 12.1 Analysis / guides

- `DetectFaceBox(...)`
- `DetectFaceLandmarks(...)`
- `EstimateClothingStartGuide(...)`

### 12.2 Region detectors

- `DetectClothingBoundary(...)`
- `DetectSkinRegion(...)`
- `DetectHairRegion(...)`

### 12.3 Masks

- `BuildPersonMask(...)`
- `BuildSkinMask(...)`
- `BuildClothingMask(...)`
- `BuildHairMask(...)`
- `BuildJawlineMask(...)`
- `BuildDoubleChinMask(...)`

### 12.4 Crop / work area

- `BuildPortraitTrainingCropBox(...)`
- `BuildNoseWorkArea(...)`
- `BuildDoubleChinWorkArea(...)`
- `BuildNeckWorkArea(...)`

### 12.5 Retouch / output

- `BlendBackground(...)`
- `ApplyToneResponse(...)`
- `ApplyTextureRetouch(...)`

## 13. Current Status Interpretation

Current stable center-face layer:
- eyes
- nose
- mouth

Current active lower-face / framing targets:
- `chin`
- `clothing_start`
- jawline
- neck band
- lower crop stability

Therefore the next detector investment should go below the face center:
- clothing boundary
- neck skin
- jawline / double-chin support
- later hairline and upper-head support

## 14. Source Notes

Primary original naming sources:
- `CURRENT_DEVELOPMENT_DIRECTION.md`
- `PERSON_MASK_AND_SUBJECT_FORMULA_SPEC.md`
- `Native/KRetouchPro.Native/README.md`

Primary current dictionary sources:
- `KRETOUCHPRO_PHOTOGRAPHIC_TERMS_DICTIONARY.md`
- `PORTRAIT_BODY_HAIR_CLOTHING_LOCATION_DICTIONARY.md`
- `PERSON_MASK_AND_SUBJECT_FORMULA_SPEC.md`

This file is the naming pillar document to guide future detector, mask, crop, and retouch implementation.
