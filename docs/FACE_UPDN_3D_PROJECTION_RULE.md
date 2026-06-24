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

Projection convention:

```text
image plane: z = 0
positive z: closer to the camera
camera projection denominator: cameraDistance - z
z scaling: apply to (correctedZ - pivotZ), not to absolute image coordinates
```

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

## 10. Rejected Relief Warp Path

The first face-line relief warp was visually rejected.

Reason:

- point controls made the face feel soft and rubber-like
- chin and facial features could drift as independent local pulls
- the result looked like liquify, not a rigid face mask

The relief warp code may remain as a fallback for other shape tools, but `Up/Dn` must not use it as the primary renderer.

## 11. Rigid 2.5D Face Mask Rule

The approved `Up/Dn` renderer is a rigid 2.5D face mask layer.

Required mental model:

```text
cut a face mask from the original image
give the whole mask a shallow synthetic face depth
keep the landmark surface topology fixed
rotate/project the mask as one firm shell
feather only the alpha edge
```

Required behavior:

- use cached MediaPipe points only
- use the 478 points as a fixed face surface, not as independent liquify handles
- preserve point-to-point structure during projection
- render source triangles into projected target triangles
- do not run local RBF, local Gaussian pull, or per-feature displacement inside `Up/Dn`
- do not independently warp eyes, nose, mouth, cheeks, or jaw
- allow only small 2.5D pitch projection and global mask movement
- keep the face interior firm
- use soft alpha feather only at the mask boundary

Boundary attach rule:

```text
rigid mask interior: fixed, plate-like
outside attach band: 10px to 30px from the mask boundary
outside movement: follow the projected boundary delta weakly
outside falloff: strongest near the boundary, fades to 0 at 30px
```

The attach band must not make the mask interior soft. It is only a small skin-adherence pass to reduce edge separation.

Depth rule:

```text
nose tip: highest
nose bridge: high
cheeks / forehead / mouth area: medium
chin: lower
jaw line / face outline: lowest
```

Current test value:

```text
synthetic nose depth max: faceWidth * 0.09
```

If MediaPipe `z` is unstable, use the approved synthetic oval depth.

## 12. Up/Dn First Implementation Scope

The rigid renderer is implemented in `Up/Dn` first.

First scope:

```text
face mask from face-line interior
include an extended full lower-jaw under-chin band
exclude hair
exclude ears
exclude shoulders
exclude clothes
```

Implementation steps:

1. build cached source landmark points
2. calculate projected points with shallow 2.5D pitch
3. build a rigid mask polygon from the face oval plus extended full lower-jaw band
4. triangulate source points once for the render request
5. render projected target triangles by inverse sampling from source triangles
6. blend with normal edge feather, and use stronger feather on the lower expanded edge
7. keep the old relief path available for non-`Up/Dn` tools

Do not move this into common FaceShape code until the `Up/Dn` visual behavior is accepted.

## 13. Face Region Scope

First `Up/Dn` rigid mask scope:

```text
face-line interior
+ full lower-jaw under-chin upper-neck mask extension
```

Excluded in first rigid mask:

- hair
- ears
- lower neck
- shoulders
- clothes
- full person mask

Reason:

- the first goal is facial pitch correction
- full head pose correction needs separate hair, neck, and body layers
- the current renderer must prove the rigid face mask first

## 14. Performance Rule

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

## 15. Fallback Rule

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
disable rigid mask rendering
return to projected point overlay or previous stable relief path
```

If cache is missing:

```text
run MediaPipe once
cache result
then continue
```

## 16. Current Implementation Checkpoint

Current approved checkpoint:

- MediaPipe worker can stay warm in RAM
- FaceShape can reuse cached 478 landmarks
- `Up/Dn` debug overlays are optional and must not block judgment when disabled
- `Up/Dn` applies a rigid 2.5D face mask layer, not a soft relief warp
- the mask includes face-line interior and a small under-chin extension
- the mask boundary uses feathered alpha only
- the face interior remains rigid
- hair, ears, lower neck, shoulders, clothes, and background are excluded from the first rigid mask
- the six rigid control anchors are still used to set the initial pivot/camera sanity frame
- build target remains x64 Debug in the program's normal output folder

Next accepted implementation step:

```text
validate the Up/Dn rigid mask renderer visually
then tune depth strength, mask polygon, edge feather, and projection strength
then move the accepted rigid renderer into common FaceShape pose code
```

## 17. Short Version

Short rule:

```text
Do not solve the impossible exact camera problem.
Use a fast portrait-camera assumption.
Use MediaPipe once and cache it.
Use z shallowly.
Use a rigid 2.5D face mask for Up/Dn.
Do not use soft local liquify controls for Up/Dn.
Feather only the edge.
```
