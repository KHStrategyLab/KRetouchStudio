# Detection Pipeline Contract

## 1. Purpose

`Detection Pipeline` defines the shared detection-first image-analysis order for KRetouchPro.

Its role is:
- separate detection input from final render input
- lock stable feature structure before any beauty enhancement
- ensure auto and manual anchors feed the same downstream detector path
- keep retouch tools dependent on resolved structure, not guessed contour lines

This pipeline is not a beauty pass.
It is the analysis contract that must run before tool apply.

## 2. Core Rule

The pipeline must always follow this order:

```text
Anchor Intake
-> Detection Prepass
-> Structure Lock
-> Original Remap
-> Tool Apply
```

Meaning:
- anchors decide where to search
- prepass decides how to simplify the image for detection
- structure lock decides the actual point / curve / mask result
- original remap sends that result back to original-image coordinates
- tool apply uses only the original image plus the confirmed result

## 3. Anchor Intake

### 3.1 Input anchor sources

Supported anchor sources:
- `Automatic`
- `ManualApproved`

Rule:
- `Manual Detect OFF` = use the automatic anchor set
- `Manual Detect ON` = use the user-approved anchor set if it exists
- if no manual-approved anchor set exists, fall back to the automatic anchor set

Rule:
- auto and manual anchor sources are different inputs
- they are not different detector pipelines
- all downstream detection stages must consume the same anchor contract

### 3.2 Current required core anchors

Current minimum:
- `left_eye`
- `right_eye`
- `nose_tip`
- `mouth_left`
- `mouth_right`
- `mouth_center`
- `chin`
- `clothing_start`

Meaning:
- these anchors are navigation inputs
- they are not final contour truth

## 4. Detection Prepass

### 4.1 Purpose

The prepass must simplify the image before structure detection.

Goal:
- remove distracting texture
- keep coarse shape contrast
- preserve dark/light structural relationships

### 4.2 Current approved direction

Current approved prepass direction:
- grayscale conversion
- mild contrast lift
- light Gaussian blur

Current production-aligned test direction:
- grayscale
- contrast gain about `1.2`
- Gaussian blur around `5x5`

This is not a final frozen constant.
It is the current approved structural-detection direction.

### 4.3 Rule

The prepass buffer is only for detection.

The prepass buffer must not become:
- the preview source image
- the export source image
- the final retouch target image

## 5. Structure Lock

### 5.1 Purpose

`Structure Lock` means finding the actual feature geometry from the simplified detect buffer.

Outputs may be:
- feature points
- polylines
- curved boundaries
- region masks
- confidence values

### 5.2 Allowed local evidence

Allowed structure cues:
- luminance contrast
- gradient magnitude
- dark-band search
- contour continuity
- ellipse or roundness cues
- local thresholding
- local fill ratio
- edge polarity

### 5.3 Forbidden shortcut

Do not treat:
- straight point interpolation
- dense contour linking
- preview-enhanced image edges

as final visible truth unless local evidence confirms them.

## 6. Original Remap

### 6.1 Purpose

After structure lock completes, the detect-buffer result must be mapped back to original-image coordinates.

This remap stage is required because:
- detection works on a simplified image
- retouch must act on the original image

### 6.2 Rule

The pipeline must keep:
- original image coordinates
- detect buffer coordinates
- ROI-local coordinates

clearly separated.

All confirmed outputs must be restored to original-image coordinates before tool apply.

## 7. Tool Apply

### 7.1 Purpose

Tool apply is the first stage allowed to touch final image content.

Examples:
- skin smoothing
- wrinkle cleanup
- lip tint
- dodge / burn
- blur / sharpen
- background replacement
- jawline or neck work

### 7.2 Rule

Tools must consume:
- original image
- confirmed structure result
- confirmed mask / ROI
- user strength value

Tools must not consume:
- raw preview image as analysis input
- sharpened detection buffer
- beautified intermediate image as a detector source

## 8. Global Forbidden Flows

The following flows are forbidden:

1. sharpen first, detect later
2. high-pass first, detect later
3. preview-adjusted image reused as detector input
4. final apply image reused as a fresh detector input
5. point-only contour interpolation treated as soft-boundary truth without local evidence

## 9. Per-Feature Routing

This contract is shared by all later feature detectors.

Expected next detector families:
- eye structure
- nose structure
- mouth structure
- jaw / neck boundary
- skin region
- clothing boundary
- hair boundary

Each feature detector should define:
- input anchors
- ROI policy
- prepass policy
- structure cue policy
- output shape
- original remap rule

## 10. Current Implementation Direction

Current safe implementation order:

1. shared anchor intake contract
2. shared detection prepass builder
3. eye structure detector
4. nose structure detector
5. mouth structure detector
6. jaw / neck boundary detector
7. region detectors that consume the same anchor and prepass policy

## 11. Short Definition

Short version:

- detect softly
- lock structure from coarse evidence
- discard the detect buffer after geometry is confirmed
- apply detail only on the original image

This is the approved detection architecture direction for KRetouchPro at the current stage.
