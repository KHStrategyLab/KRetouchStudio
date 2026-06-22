# Anchor / Region / Crop / Retouch Layer

## 1. Anchor Layer

Purpose:
Provide the stable point-based face skeleton used by crop logic, preview guidance, and later local retouch work.

Components:
- Automatic landmarks for eyes, nose, and mouth
- Manual 8-point anchors:
  - `left_eye`
  - `right_eye`
  - `nose_tip`
  - `mouth_left`
  - `mouth_right`
  - `mouth_center`
  - `chin`
  - `clothing_start`

Current policy:
- Eyes, nose, and mouth are already strong enough for practical production use.
- `chin` is the main lower-face adjustment anchor.
- `clothing_start` is not yet a real clothing detector result.
- `clothing_start` currently acts as a lower crop guide anchor.
- The current 7 face points plus `clothing_start` are navigation anchors only.
- They should define approximate region entry boxes, not final visible boundaries.
- Dense `106-point` contour linking is a later candidate, not the active core path.
- `Manual Detect` is an anchor-input switch, not a separate detector pipeline.
- `Manual Detect OFF` means the engine continues with the current automatic 8-anchor result.
- `Manual Detect ON` means the user-approved 8 anchors replace the automatic anchor set as the next detector input.
- If manual anchors are not provided, the engine must continue from the automatic anchor set without blocking later region detectors.

Reason:
- Point-to-point linking can make jawline and lip boundaries look angular.
- Soft curved edges should be found from local pixel evidence inside the guided region.
- The engine should use anchors to enter the right zone, then let local formulas find the real boundary.
- This lighter path preserves headroom for zero-latency preview, higher-quality render passes, faster batch work, and cheaper hardware targets.

## 2. Region Detector Layer

Purpose:
Add area-based understanding on top of point-based anchors.

This layer should be separated by intent:

### 2.1 Skin Detector

Use cases:
- Under-jaw skin band
- Neck skin region
- Face-to-background edge support
- Double-chin and neck wrinkle work areas

Role:
- Support jawline and neck decisions
- Support local retouch masks

### 2.2 Clothing Detector

Use cases:
- Neck-to-clothing boundary
- Clothing start line
- Shoulder entry region

Role:
- Evolve `clothing_start` from a guide point into a detector-backed boundary
- Support lower crop stability
- Support portrait framing and clothing protection masks

### 2.3 Hair Detector

Use cases:
- Hairline
- Upper head contour
- Ear-side hair boundary

Role:
- Support top crop decisions
- Support hair protection during background replacement and portrait cleanup

Note:
- Hair should come after skin and clothing because its variation is larger.

## 3. Crop Layer

Purpose:
Build stable training and work-area crop boxes from anchor and detector information.

Current crop inputs:
- Eye center
- `chin`
- `clothing_start`

Current behavior:
- Top is controlled by eye center and eye-to-chin distance
- Bottom is stabilized by the larger of:
  - the default eye-to-chin extension
  - the `clothing_start` guide plus margin

Current meaning of `clothing_start`:
- Not clothing segmentation
- Not a full clothing mask
- A lower framing guide that helps stabilize the portrait training crop box

## 4. Retouch Work Area Layer

Purpose:
Create task-specific local work regions before applying any warp, mask, or filter.

Examples:
- Double chin:
  - centered from `chin`
  - expanded into the under-jaw skin region
- Neck wrinkle cleanup:
  - bounded around the neck band above `clothing_start`
- Background replacement:
  - bounded by person mask rules

Rule:
- Every slider should define its own work area first.
- The full preview must not be treated as the default work area.

## 5. Retouch Engine Layer

Purpose:
Run the actual image processing after anchors, regions, and work areas are defined.

Engine categories:
- Warp
  - jawline
  - face shape
  - asymmetry correction
- Mask / Blend
  - skin
  - background
  - clothing boundary
- Filter
  - skin cleanup
  - tone adjustment
  - sharpness / blur

Rule:
- Anchors define structure
- Detectors define regions
- The engine applies the edit
- Navigation anchors do not define the final contour by themselves
- Local formulas inside the ROI should define the real edge
- Manual and automatic anchor sources must converge into the same downstream detector path

## 6. Current Practical Conclusion

Stable now:
- Eyes
- Nose
- Mouth

Main active refinement targets:
- Chin
- Lower crop stability
- Clothing-start guidance

Current interpretation:
- The center-face landmark system is already good enough for real studio work.
- The next meaningful gains come from jawline, lower framing, neck, and clothing boundary handling.

## 7. Next Evolution Order

1. Keep `clothing_start` as a guide anchor in the current production path
2. Keep the face points as navigation-only anchors for eye, nose, mouth, and chin regions
3. Add local formula detection inside each guided region
4. Add clothing-boundary detection around `clothing_start`
5. Add skin detection under the jaw and on the neck band
6. Add hair detection after skin and clothing are stable

## 8. Working Principle

Short version:

- Anchor layer = points
- Anchor layer = navigation
- Region detector layer = areas
- Crop layer = framing
- Retouch work area layer = task-specific local box
- Retouch engine layer = actual processing

Current direction:

- `7-point / 8-point` = navigation
- `local formulas` = true boundary
- `106-point dense contour` = later candidate path

This is the intended architecture direction for KRetouchPro at the current stage.
