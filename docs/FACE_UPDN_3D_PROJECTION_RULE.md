# Face Up/Dn 3D Projection Rule

## 1. Purpose

This document fixes the approved direction for `Facial Reshape > Head Pose > Up/Dn`.

It exists to prevent the engine from drifting into the wrong problem:

- do not chase mathematically perfect lens and distance recovery from a single 2D photo
- do not turn `Up/Dn` into a flat vertical displacement warp
- do not use all 478 MediaPipe points blindly as a camera solver or heavy per-commit warp input
- do not rebuild a heavy 3D system before the visual behavior is proven

The target is a fast, visually natural 2.5D portrait correction that looks like a small head pitch change.

## 2. Reality Check

Exact camera lens, subject distance, and physical face depth cannot be recovered perfectly from:

```text
one 2D portrait image
+ MediaPipe 478 normalized landmarks
```

Reason:

- the engine does not know the subject's real face size in centimeters
- a large face photographed far away and a small face photographed closer can share similar 2D landmark layouts
- MediaPipe `z` is a relative landmark depth hint, not a calibrated studio depth map
- EXIF can help, but EXIF may be missing, wrong, stripped, or insufficient

Therefore, the engine must not block on perfect camera reconstruction.

Approved target:

```text
stable visual correction
fast preview
safe fallback
small controllable pitch illusion
```

## 3. Approved Priority

Priority order:

1. visual naturalness
2. preview speed
3. stable fallback behavior
4. reusable cached landmarks
5. camera plausibility
6. mathematical completeness

If a mathematically elegant method is slow, unstable, or visually wrong, reject it.

## 4. Camera Assumption Rule

The engine may use real camera metadata when available, but must not require it.

Camera source order:

1. EXIF focal length and sensor data, if reliable
2. studio-standard fallback camera
3. simple portrait default

Fallback camera:

```text
portrait lens feel: 50mm to 85mm equivalent
moderate perspective
no aggressive wide-angle distortion
no flat telephoto overcorrection
```

If EXIF is unavailable:

```text
assume standard studio portrait camera
assume oval face volume
assume nose is the nearest facial feature
assume eye and mouth regions sit behind the nose
```

The program must continue without error.

## 5. solvePnP Rule

OpenCV `solvePnP` is allowed only as an initial camera-pose estimator.

Approved use:

- run once after image load or first edit-mode landmark detection
- use a small rigid point set
- cache the estimated pose
- use it as a hint, not final truth

Required first rigid control points:

```text
nose tip: 4
chin: 152
left eye outer corner: 33
right eye outer corner: 263
left mouth corner: 61
right mouth corner: 291
```

Forbidden use:

- do not run `solvePnP` during every slider movement
- do not feed all 478 points into `solvePnP`
- do not trust mouth, eyelid, or expression-moving points as rigid camera truth
- do not let bad pose estimation crash or block the edit

If `solvePnP` is unstable:

```text
ignore it
fall back to the standard studio camera assumption
```

## 6. Landmark Runtime Rule

MediaPipe is a detector, not a per-frame retouch loop.

Required flow:

```text
Program start
-> warm MediaPipe worker in RAM

Image load or first edit-mode use
-> run MediaPipe once
-> cache 478 landmarks and relative z values

Slider commit
-> read cached landmarks
-> project or warp from cached data
-> do not run MediaPipe again
```

The current approved runtime direction is:

- MediaPipe worker stays resident
- first detection result is cached per photo path
- retouch slider code reads cached landmark data only

## 7. Up/Dn First Experiment

The first approved `Up/Dn` experiment is not pixel warp.

It is a projected debug overlay only.

The debug overlay must show two sets of points on a dedicated `Up/Dn` layer:

```text
source MediaPipe landmarks
projected MediaPipe landmarks after shallow 2.5D pitch projection
```

The six rigid control points define the initial sanity anchors:

```text
nose tip: 4
chin: 152
left eye outer corner: 33
right eye outer corner: 263
left mouth corner: 61
right mouth corner: 291
```

Rules:

- draw all cached MediaPipe points for visual debugging
- keep the six control anchors as pivot/camera sanity inputs
- do not use iris or eye-center points as the rigid eye reference
- do not draw face box
- draw only the dedicated projected feature guide lines needed for visual judgment
- mutate pixels only inside the face-line relief layer after projection direction is accepted
- add history only when pixel warp is applied

This phase exists to verify direction, sensitivity, pivot behavior, depth distribution, and the first face-line-only relief warp.

## 8. Projection Method

The approved first math path is:

```text
MediaPipe landmark x/y/z
-> shallow z correction
-> pivot behind nose / inside face volume
-> BackProject
-> Pitch Rotate
-> Project
-> draw projected debug points
```

This is the middle path between the old `Pro` system and the failed displacement warp.

Bring from old `Pro`:

- `BackProject -> Rotate -> Project`
- camera center, camera distance, focal length
- mesh surface preservation
- zero-pose drift check

Do not bring from old `Pro`:

- `rotationPivotImage = chin`
- rough 8-point fake depth as final truth
- large full-face influence region

Bring from current MediaPipe work:

- 478 detected landmark positions for debug projection and later mesh construction
- MediaPipe relative z values
- actual eye, nose, mouth, face-line locations
- debug overlay verification

Do not bring from the failed current warp:

- plain x/y displacement
- excessive nose-only z influence
- lower-face fixed pins that block rotation
- whole-face flat-card behavior

## 9. Depth Rule

MediaPipe z must be used shallowly.

Do:

- preserve the sign and relative ordering
- treat nose as nearest
- treat eyes and mouth as behind the nose
- clamp extreme z
- scale z down before projection

Do not:

- multiply raw z aggressively
- let the nose become a long protruding spike
- let the face stretch apart like separate stickers
- let mouth or eyes detach from the face surface

Approved mental model:

```text
soft oval face surface
nose is slightly raised
eye and mouth regions are on the same flexible surface
pitch rotates the surface, not individual features
```

## 10. Relief Warp Path

Only expand after the all-point projected debug layer looks right.

Approved order:

1. draw all source and projected MediaPipe points on the dedicated `Up/Dn` debug layer
2. use the six rigid control anchors to judge pivot and sensitivity
3. add projected face-line sample points
4. add eye, nose, mouth local sample points
5. draw projected eye, eyebrow, and mouth guide lines for visual checking
6. validate zero-pose drift
7. add pixel warp inside face-line only
8. blend with an inner feather around the face-line boundary
9. add optional camera-pose hint from `solvePnP`

Do not jump directly from point projection to full image warp.

## 11. Face Region Scope

First `Up/Dn` warp scope:

```text
face-line interior only
```

Excluded in first warp:

- hair
- ears
- neck
- shoulders
- clothes
- full person mask

Reason:

- head tilt involving hair, ears, neck, and clothes is a different tool
- first goal is facial pitch correction, not full head pose correction

## 12. Performance Rule

Studio use requires immediate response.

Required:

- no detector call during slider movement
- no detector call during slider commit if cache exists
- no all-478 expensive solve per commit
- all-478 point drawing is allowed only as a lightweight debug visualization from cached landmarks
- avoid allocating large temporary objects inside tight loops
- keep preview work bounded to face region
- use parallel pixel work only after the math path is visually approved

Reject any implementation that makes the slider feel heavy.

## 13. Fallback Rule

The engine must always have a safe fallback.

If EXIF is missing:

```text
use standard portrait camera
```

If solvePnP fails:

```text
ignore solvePnP and use standard portrait camera
```

If MediaPipe z is unstable:

```text
use shallow oval face depth
```

If projection looks visually wrong:

```text
do not warp pixels
return to point-only overlay
```

If cache is missing:

```text
run MediaPipe once
cache result
then continue
```

## 14. Current Implementation Checkpoint

Current approved checkpoint:

- MediaPipe worker can stay warm in RAM
- FaceShape can reuse cached 478 landmarks
- `Up/Dn` debug path must hide normal MediaPipe face boxes and feature lines
- `Up/Dn` debug path displays all cached MediaPipe source points and projected points on a dedicated layer
- `Up/Dn` debug path does not display projected triangle wireframe lines
- `Up/Dn` debug path displays projected eye, eyebrow, outer-mouth, and inner-mouth guide lines for visual judgment
- `Up/Dn` applies a first face-line-only relief warp by blending a warped face interior over the original image
- the face-line boundary uses an inner feather and does not move hair, ears, neck, shoulders, clothes, or background
- the six rigid control anchors are still used to set the initial pivot/camera sanity frame
- build target remains x64 Debug in the program's normal output folder

Next accepted implementation step:

```text
validate the face-line-only relief warp visually
then tune point-to-point weighting, feather width, and projection strength
```

## 15. Short Version

Short rule:

```text
Do not solve the impossible exact camera problem.
Use a fast portrait-camera assumption.
Use MediaPipe once and cache it.
Use z shallowly.
Verify Up/Dn with all source/projected points first.
Warp only the face-line interior as a shallow relief layer over the original image.
```
