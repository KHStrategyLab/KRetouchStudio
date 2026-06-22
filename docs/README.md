# KRetouch Studio Docs

## Active Reading Order

1. `LOCAL_PROXY_WORKBENCH_DESIGN.md` - CS3-style local proxy workbench design.
2. `SCREEN_IO_AND_WORK_MODE_ARCHITECTURE.md` - screen runtime mode, panel-state, layer/history, and tool-file separation policy.
3. `TOOLBOX_RETOUCH_PANEL_CONTRACT.md` - toolbox target ownership and retouch-panel parameter dispatch boundary.
4. `DETECTION_PIPELINE_CONTRACT.md` - shared detection-first pipeline contract from anchor intake to original apply.
5. `FACEBOX_DEPTH_ROLE_MODEL.md` - lightweight 2.5D facial volume interpretation from a frontal face box.
6. `FACE_ROTATION_DEPTH_RULE.md` - face rotation policy derived from the FaceBox depth-role model instead of flat 2D image spin.
7. `DETECTOR_TARGET_AND_MASK_IDS.md` - code-facing detector target, search ROI, work box, and mask ID catalog.
8. `PERSON_MASK_AND_SUBJECT_FORMULA_SPEC.md` - canonical PersonMask, SubjectMask, BioMask, and background formula.
9. `KRETOUCHPRO_PHOTOGRAPHIC_TERMS_DICTIONARY.md` - photographic and retouch-routing vocabulary.
10. `PORTRAIT_BODY_HAIR_CLOTHING_LOCATION_DICTIONARY.md` - body, hair, clothing, and region naming dictionary.
11. `PORTABILITY_AND_SETUP.md` - checkout-root, dependency, and local-state rules for other computers.
12. `session_logs/2026-06-15.md` - migration diary and historical direction notes.
13. `session_logs/2026-06-16.md` - 8-point approval crop policy decision log.

## CORE Documents

- [공식](공식.md) - core formula constitution.
- [CORE Formula Companion](CORE_FORMULA_COMPANION.md) - core detection policy, zone-mask explanation, and rationale archive.
- [CORE 2D Render Tricks](CORE_2D_RENDER_TRICKS.md) - lightweight final-render upgrade tricks.
- [Detection Pipeline Contract](DETECTION_PIPELINE_CONTRACT.md) - shared detection-first analysis order and original-remap rule.
- [FaceBox Depth Role Model](FACEBOX_DEPTH_ROLE_MODEL.md) - frontal `FaceBox` interpreted as a lightweight 2.5D facial volume container.
- [Face Rotation Depth Rule](FACE_ROTATION_DEPTH_RULE.md) - future face rotation follows role-guided local depth behavior instead of flat image rotation.
- [Detector Target And Mask IDs](DETECTOR_TARGET_AND_MASK_IDS.md) - code-facing ID split for detector targets, search ROI, work boxes, and reusable masks.
- [Face Visible Structure Master](FACE_VISIBLE_STRUCTURE_MASTER.md) - one-sheet visible face structure vocabulary for detection, mask, and tool routing.
- [Screen I/O And Work Mode Architecture](SCREEN_IO_AND_WORK_MODE_ARCHITECTURE.md) - screen mode, panel state, layer/history split, and tool-file policy.
- [Toolbox And Retouch Panel Contract](TOOLBOX_RETOUCH_PANEL_CONTRACT.md) - toolbox target input and right-panel parameter dispatch contract.

## Skin Detection And Mask Contracts

- [DetectSkinRegion Contract](DETECT_SKIN_REGION_CONTRACT.md) - average-skin candidate detection, feature subtraction, and resolved skin-region definition.
- [BuildSkinMask Contract](BUILD_SKIN_MASK_CONTRACT.md) - stabilized reusable `SkinMask` construction from resolved skin-region evidence.
- [BuildFaceSkinMask Contract](BUILD_FACE_SKIN_MASK_CONTRACT.md) - face-only skin mask split from broader skin evidence.
- [BuildSkinSafeMask Contract](BUILD_SKIN_SAFE_MASK_CONTRACT.md) - safe retouch execution mask after subtracting protected non-skin structures.
- [Clothing Boundary / Skin Mask Flow](CLOTHING_BOUNDARY_SKIN_MASK_FLOW.md) - detector-to-mask pipeline flow across clothing boundary and skin-region stages.

## Current Build Goal

- Rebuild the main project as a small, controlled retouch engine shell.
- Current implementation target: source-aligned display-only Local Proxy Workbench overlay.
- Current connected tool entries: `DoubleChin` and `Nose Shape`.
- WorkArea is defined in original-image coordinates.
- LocalProxy is generated from the WorkArea, then displayed in the overlay.
- Local proxy sizing currently follows source buckets:
  - `1200` for `FullFrameProxy`
  - `384 / 512 / 768 / 1024` for `LocalWorkAreaProxy`

## Documentation Path Policy

- Use repository-relative links inside `docs/`.
- Do not hard-code machine-specific absolute paths in doc indexes or cross-links.
- Treat source code as the authority when design notes and implementation differ.

## Current Landmark Direction

- Active direction: `about 8 core points + smart local detection formulas`.
- The current 7 face points plus `clothing_start` are navigation anchors, not final contour truth.
- Their job is to define approximate ROI boxes for eyes, nose, mouth, chin, and lower framing.
- The true boundary inside each ROI should be found by local image evidence:
  - gradient
  - luminance
  - saturation
  - alpha / soft-edge behavior
- Reason:
  - dense point-to-point contour linking can force straight or angular boundaries
  - this is especially weak for jawline, lips, and other curved soft edges
  - the engine should not treat point interpolation as the final visible contour
  - the lighter path leaves more compute headroom for real-time preview, better final image quality, faster batch processing, and lower hardware cost
- Therefore:
  - dense `106-point` landmarking is moved to a later candidate path
  - it is not the current core detection architecture

## Current Implementation Rule

```text
OriginalImage
-> WorkArea
-> WorkAreaCrop
-> LocalProxy
-> LocalMask
-> LocalPreview
-> Apply or Cancel
```

The main preview is the full-image result view.
The local proxy workbench is the temporary local retouch workspace.

## Manual Approval Points

- The current approval layer uses 8 draggable points.
- `Manual Detect` is an anchor-source switch for the same downstream detector chain.
- `Manual Detect OFF` = use the automatic 8-anchor set.
- `Manual Detect ON` = use the user-approved 8-anchor set.
- If the user does not define manual anchors, later detection stages must continue from the automatic anchor set.
- Face approval points are 7 points:
  - `left_eye`
  - `right_eye`
  - `nose_tip`
  - `mouth_left`
  - `mouth_right`
  - `mouth_center`
  - `chin`
- Clothing guide point is separate from the face points:
  - `clothing_start`
- `chin` is vertical-axis locked.
- `clothing_start` is also vertical-axis locked because it is a clothing-start guide, not a face landmark.

## PortraitTrainingCropBox Policy

- Training export source must use `PhotoItem.BaseImage`, the original loaded image.
- The main preview image is not used as the export crop source.
- The approved 8 points are data points, not the crop rectangle itself.
- `PortraitTrainingCropBox` is the crop rectangle used for face 8-point learning and clothing-detection formula verification.
- `head_top` is not stored as an approval point.
- The training crop rectangle is computed at Enter/export time.
- Current crop policy id:
  - `portrait_training_crop_box_v1_clothguide_v2`
- Crop rectangle formula:
  - `D = ChinY - EyeCenterY`
  - `Top = EyeCenterY - 1.0D`
  - `BottomBase = EyeCenterY + 2.0D`
  - `Bottom = max(BottomBase, ClothingGuideY + 3)`
  - `Height = Bottom - Top`
  - `Width = Height * 2 / 3`
  - `CenterX = EyeCenterX`
  - `Left = CenterX - Width / 2`
  - `Right = CenterX + Width / 2`
- `clothing_start` stays in the JSON landmark data and is also used as the lower safety guide for the current crop policy.
- `clothing_start` initial placement is seeded closer to the neckline:
  - `YuNetChinY + FaceBoxHeight * 0.37`
- Eye approval handles use larger visual circles for pupil-checking.
