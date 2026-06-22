# Detector Target And Mask IDs

## 1. Purpose

This document defines the code-facing ID catalog that sits between:
- `DETECTION_PIPELINE_CONTRACT.md`
- `FACE_VISIBLE_STRUCTURE_MASTER.md`
- mask-building and tool-apply code

Its job is:
- separate analysis-side detector targets from apply-side mask IDs
- define `SearchROI`, `WorkBox`, and `FinalMask` as different layers
- give the codebase one stable naming list for future detector and tool wiring

## 2. Core Split

The engine must keep these four things separate:

### 2.1 `DetectorTargetId`

Meaning:
- a visible structure the detector tries to lock from pixels
- analysis-side only

Naming:
- lower snake case

Examples:
- `nose_bridge_center_band`
- `left_nostril_dark_pocket`
- `mouth_slit_polyline`

### 2.2 `SearchRoiId`

Meaning:
- the broad search region used to scan for one detector family
- usually derived from navigation anchors
- wider than the final structure

Naming:
- lower snake case

Examples:
- `nose_search_roi`
- `mouth_search_roi`

### 2.3 `WorkBoxId`

Meaning:
- the edit box or tool-routing box for a feature
- may be rectangular, rounded, or soft-edged in UI
- may be larger than the final detected structure

Naming:
- lower snake case

Examples:
- `nose_work_box`
- `double_chin_work_box`

### 2.4 `MaskId`

Meaning:
- a resolved reusable pixel-domain mask
- apply-side object
- may be used as `ApplyMask`, `ProtectMask`, or `BlockMask`

Naming:
- PascalCase

Examples:
- `NoseMask`
- `JawlineMask`
- `ClothingMask`

## 3. Spatial Rule

Correct order:

```text
Anchor Intake
-> SearchROI
-> DetectorTarget lock
-> FinalMask resolve
-> WorkBox routing
-> ApplyMask / ProtectMask / BlockMask composition
-> Tool Apply
```

Meaning:
- `SearchROI` is not the final feature
- `WorkBox` is not the final feature
- `FinalMask` is the actual pixel ownership result
- later tool masks may reuse the same `MaskId` or combine several `MaskId`s

## 4. Nose Example

For the nose, the user may visually think in terms of a red rounded zone.
That zone should be defined as a `WorkBox`, not as the final nose boundary.

### 4.1 `nose_search_roi`

Broad analysis region that includes:
- upper bridge support
- side planes
- nose tip
- alar bodies
- nostril dark pockets
- philtrum start

Role:
- detector scan region only

### 4.2 `nose_work_box`

Tool edit box used for nose correction.

This box may include:
- full visible nose surface
- lower bridge
- tip mass
- alar width zone
- nostril base
- upper philtrum start

Rule:
- it may be slightly larger than the visible nose
- it may be shown as a rounded correction box in the preview
- it must not be mistaken for `NoseMask`

Current builder contract:

```text
EyeCenterX = (LeftEyeX + RightEyeX) / 2
EyeCenterY = (LeftEyeY + RightEyeY) / 2
EyeDistance = Distance(LeftEye, RightEye)
MouthCenterY = MouthCenterY if available, else NoseTipY + 0.62 * EyeDistance
CenterX = EyeCenterX * 0.40 + NoseTipX * 0.60

Left   = CenterX - 0.32 * EyeDistance
Right  = CenterX + 0.32 * EyeDistance
Top    = EyeCenterY - 0.08 * EyeDistance
Bottom = max(NoseTipY + 0.26 * EyeDistance, MouthCenterY - 0.19 * EyeDistance)
```

Meaning:
- top starts slightly below the inter-eye line
- left and right include the alar width with small breathing room
- bottom reaches through the nostril base and upper philtrum entry
- the box is a tool work box, not the final pixel mask

Function owner:
- `BuildNoseWorkArea(...)`
- fallback owner: `BuildNoseWorkAreaFallback(...)`

### 4.3 `NoseMask`

Resolved pixel mask for the visible nose surface.

Rule:
- `NoseMask` is the final ownership mask
- sub-feature masks such as `NoseBridgeMask`, `NoseTipMask`, `LeftAlarMask`, and `RightNostrilMask` may exist inside it

## 5. Naming Rules

### 5.1 Detector-side IDs

Use:
- `left_...`
- `right_...`
- visible-structure wording first
- one target = one meaning

Avoid:
- mixing box names and final mask names
- using `mask` in detector target IDs

### 5.2 Mask-side IDs

Use:
- PascalCase
- feature or ownership meaning first
- reusable names

Avoid:
- detector-only wording inside `MaskId`
- one-off UI wording as mask names

## 6. Code-Ready Search ROI IDs

### 6.1 Face structure ROIs

- `face_skin_search_roi`
- `left_eye_search_roi`
- `right_eye_search_roi`
- `nose_search_roi`
- `mouth_search_roi`
- `jaw_neck_search_roi`

### 6.2 Accessory ROIs

- `glasses_search_roi`
- `left_lens_search_roi`
- `right_lens_search_roi`

## 7. Code-Ready Work Box IDs

### 7.1 Feature work boxes

- `eye_work_box`
- `nose_work_box`
- `mouth_work_box`
- `jawline_work_box`
- `double_chin_work_box`
- `neck_work_box`

### 7.2 Accessory and surface work boxes

- `glasses_work_box`
- `face_skin_work_box`
- `background_work_box`

## 8. Code-Ready Detector Target IDs

### 8.1 Brow and eye targets

- `left_eyebrow_body`
- `right_eyebrow_body`
- `left_brow_lower_edge`
- `right_brow_lower_edge`
- `left_upper_eyelid_curve`
- `right_upper_eyelid_curve`
- `left_lower_eyelid_curve`
- `right_lower_eyelid_curve`
- `left_lash_band`
- `right_lash_band`
- `left_sclera_region`
- `right_sclera_region`
- `left_iris_boundary`
- `right_iris_boundary`
- `left_pupil_center`
- `right_pupil_center`
- `left_inner_corner_notch`
- `right_inner_corner_notch`
- `left_under_eye_shadow_band`
- `right_under_eye_shadow_band`
- `left_corneal_highlight`
- `right_corneal_highlight`

### 8.2 Nose targets

- `nose_bridge_center_band`
- `nose_bridge_axis`
- `left_nose_side_plane`
- `right_nose_side_plane`
- `nose_tip_mass`
- `nose_tip_highlight_core`
- `left_alar_outer_edge`
- `right_alar_outer_edge`
- `left_alar_body`
- `right_alar_body`
- `left_nostril_dark_pocket`
- `right_nostril_dark_pocket`
- `nostril_lower_boundary`
- `septum_base_support`
- `philtrum_center_path`

### 8.3 Mouth targets

- `mouth_width_span`
- `mouth_slit_polyline`
- `left_mouth_corner`
- `right_mouth_corner`
- `cupid_bow_left_peak`
- `cupid_bow_right_peak`
- `cupid_bow_center_dip`
- `upper_lip_outer_contour`
- `lower_lip_outer_contour`
- `upper_lip_body_region`
- `lower_lip_body_region`
- `lower_lip_highlight_zone`
- `under_lip_shadow_band`

### 8.4 Cheek and lower-face targets

- `left_nasolabial_fold_band`
- `right_nasolabial_fold_band`
- `left_cheek_plane`
- `right_cheek_plane`
- `left_cheekbone_band`
- `right_cheekbone_band`
- `jawline_contour_band`
- `chin_center`
- `chin_lower_edge_band`
- `under_jaw_shadow_band`
- `double_chin_soft_bulge`
- `neck_skin_region`
- `clothing_start_boundary`

### 8.5 Glasses targets

- `glasses_owner_region`
- `left_frame_outer_contour`
- `right_frame_outer_contour`
- `left_lens_boundary`
- `right_lens_boundary`
- `glasses_bridge_segment`
- `left_nose_pad`
- `right_nose_pad`
- `left_glare_patch`
- `right_glare_patch`
- `frame_shadow_band`

### 8.6 Skin and support targets

- `face_skin_candidate_region`
- `shadow_band_candidate`
- `specular_highlight_patch`

## 9. Code-Ready Mask IDs

### 9.1 Ownership masks

- `PersonMask`
- `SubjectMask`
- `BackgroundMask`
- `BioMask`
- `HeadCarrierMask`
- `PersonAlpha`
- `HairAlpha`

### 9.2 Skin, hair, body, and clothing masks

- `SkinMask`
- `FaceSkinMask`
- `BodySkinMask`
- `HairMask`
- `HairlineMask`
- `HairColorTargetMask`
- `ClothingMask`
- `SleeveMask`
- `AccessoryMask`
- `EarMask`
- `LeftEarMask`
- `RightEarMask`
- `NeckBioMask`
- `ShoulderBioMask`
- `BeardMask`
- `FacialHairMask`
- `LeftHandMask`
- `RightHandMask`

### 9.3 Eye and brow masks

- `EyeMask`
- `LeftEyeMask`
- `RightEyeMask`
- `EyebrowMask`
- `EyelashMask`
- `IrisMask`
- `PupilMask`
- `UndereyeMask`

### 9.4 Nose and mouth masks

- `NoseMask`
- `NoseBridgeMask`
- `NoseTipMask`
- `LeftAlarMask`
- `RightAlarMask`
- `LeftNostrilMask`
- `RightNostrilMask`
- `PhiltrumMask`
- `LipMask`
- `UpperLipMask`
- `LowerLipMask`
- `MouthInnerMask`
- `ToothMask`

### 9.5 Lower-face and neck masks

- `JawlineMask`
- `ChinMask`
- `UnderJawMask`
- `DoubleChinMask`
- `NeckMask`
- `ClothingStartMask`
- `NasolabialMask`
- `CheekMask`
- `ShoulderMask`

### 9.6 Glasses and accessory masks

- `GlassesMask`
- `FrameMask`
- `LensMask`
- `AccessoryProtectMask`

## 10. Current Bridge Rule For Tools

Tools should bind to mask IDs like this:

- `ApplyMask` = the active edit ownership
- `ProtectMask` = structures that must stay stable
- `BlockMask` = areas that must not receive the edit

Example:

```text
Nose shaping
ApplyMask   = NoseMask
ProtectMask = LeftEyeMask + RightEyeMask + LipMask
BlockMask   = GlassesMask + BeardMask
```

Another example:

```text
Double chin
ApplyMask   = DoubleChinMask
ProtectMask = JawlineMask + BeardMask
BlockMask   = ClothingMask + AccessoryMask
```

## 11. Short Rule

Short version:

- detector target = what to find
- search ROI = where to search
- work box = where the tool may operate
- mask ID = what pixels belong to the resolved structure

This is the current code-facing ID direction for KRetouchPro.
