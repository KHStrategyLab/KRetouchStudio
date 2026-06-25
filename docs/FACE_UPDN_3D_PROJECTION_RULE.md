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
-> cache detected landmark X/Y positions
-> use canonical 468 Z lookup for rigid mask depth

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

- detected landmark X/Y positions for debug projection and later mesh construction
- canonical 468 face model Z values for rigid depth
- actual eye, nose, mouth, face-line locations
- debug overlay verification

Do not bring from the failed current warp:

- plain x/y displacement
- excessive nose-only z influence
- lower-face fixed pins that block rotation
- whole-face flat-card behavior

## 9. Depth Rule

Photo-detected MediaPipe z must not drive the first rigid `Up/Dn` face mask.
It changes too much by detector state and made the surface unstable.

Do:

- use detected MediaPipe X/Y for the current photo
- use official `canonical_face_model.obj` Z for the half-3D face surface
- use official `canonical_face_model.obj` face topology for the rigid mask mesh
- normalize canonical Z to the current face width
- treat nose tip index 4 as the nearest point
- exclude the extra iris points from rigid mask geometry

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
pull the face landmarks backward/forward with canonical 468 depth
keep the landmark surface topology fixed
rotate/project the mask as one firm shell
feather only the alpha edge
```

Required behavior:

- use cached MediaPipe points only
- use the canonical 468 face points as a fixed face surface, not as independent liquify handles
- ignore the extra iris points for rigid mask geometry
- preserve point-to-point structure during projection
- render source triangles into projected target triangles
- do not run local RBF, local Gaussian pull, or per-feature displacement inside `Up/Dn`
- do not independently warp eyes, nose, mouth, cheeks, or jaw
- allow only small 2.5D pitch projection and global mask movement
- keep the face interior firm
- use soft alpha feather only at the mask boundary

Topology reference images:

```text
docs/reference_images/relief_mesh/front_topology_reference.jpg
docs/reference_images/relief_mesh/front_point_topology_reference.jpg
docs/reference_images/relief_mesh/side_topology_reference.jpg
```

These images are reference material for a later manual 468-point relief topology pass. They are not the current runtime mesh. Use them to design human-readable flow lines and point groupings after the official MediaPipe canonical topology has been tested.

Boundary attach rule:

```text
rigid mask interior: fixed, plate-like
outside attach band: 10px to 50px from the mask boundary
outside movement: follow the projected boundary delta weakly
outside falloff: strongest near the boundary, fades to 0 at 50px
upper mask edge alpha feather: 8px
```

The attach band must not make the mask interior soft. It is only a small skin-adherence pass to reduce edge separation.
The attach band must also bridge the source-mask interior gap that appears when the projected rigid mask moves away from the original forehead edge. Skip only pixels already covered by the projected rigid mask, not every pixel inside the original source polygon.

Depth rule:

```text
source X/Y: detected MediaPipe image positions
source Z: official MediaPipe canonical_face_model.obj vertex Z
source topology: official MediaPipe canonical_face_model.obj face list
canonical point count: 468
canonical face count: 898
extra iris points: excluded from the rigid mask
nose tip index 4: maximum canonical Z
side/outline low points: pulled backward
chin/jaw/mouth/forehead: use their canonical Z, not photo-detected MediaPipe Z
```

Current test value:

```text
canonical normalized nose depth max: faceWidth * 0.09
Up/Dn pitch max: 8.75 degrees at slider 0 or 100
Up/Dn pitch pivot X/Y: MediaPipe face landmark 8, the brow-center pivot point
Up/Dn pitch pivot Z: 0.0 on the image plane
Face Turn yaw pivot X/Y: MediaPipe face landmark 8, shared with Up/Dn
Face Turn yaw pivot Z: 0.0 on the image plane
```

Do not use photo-detected MediaPipe `z` for the first rigid Up/Dn renderer. It is unstable for this task.
Normalize the official canonical Z range to the current face width, then render the rigid face mask with the official canonical face topology.
Do not insert a separate under-chin guard band into the face mask polygon. The current test mask extends only the lower half of the face oval downward by `faceHeight * 0.03`, then relies on edge feather and the outside attach band for the join.

Jaw vertical compensation rule:

```text
jaw band: 400, 377, 152, 148, 176
measure average source Y and projected Y after rigid pitch projection
if the projected jaw band rises, move the entire projected mask down by the rise * 0.85
if the projected jaw band drops, pull back only weakly by the drop * 0.35
maximum compensation: faceHeight * 0.10
```

This compensation is a global mask offset, not a local liquify warp. Apply the same Y offset to every projected mask vertex and to the projected mask polygon. Do not move eyes, nose, mouth, cheeks, or jaw independently.

## 12. Up/Dn First Implementation Scope

The rigid renderer is implemented in `Up/Dn` first.

First scope:

```text
face mask from face-line interior
extend only the lower half of the face oval downward by faceHeight * 0.03
exclude hair
exclude ears
exclude shoulders
exclude clothes
```

Implementation steps:

1. build cached source landmark points
2. calculate projected points with shallow 2.5D pitch
3. build a rigid mask polygon from the face oval, with only a small downward lower-half extension
4. render official canonical face triangles for the 468-point face shell
5. render projected target triangles by inverse sampling from source triangles
6. blend with normal edge feather, and use stronger feather on the lower expanded edge
7. keep the old relief path available for non-`Up/Dn` tools

Do not move this into common FaceShape code until the `Up/Dn` visual behavior is accepted.

## 13. Face Region Scope

First `Up/Dn` rigid mask scope:

```text
face-line interior
+ lower-half oval extension only
+ 50px outside attach band
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
- at slider center, keep `Up/Dn` on the same rigid render path instead of switching to a raw-source reset path
- at slider center, keep `Face Turn` on the same live preview route instead of clearing the drag preview

Reject any implementation that makes the slider feel heavy.

## 15. Face Turn Accepted Lock

`Facial Reshape > Head Pose > Turn` is visually accepted and locked as of the current tuning pass.

Do not change the Turn pivot, direction, projection strength, contour depth rule, central depth shift, or edge behavior without new explicit approval.

The approved Turn behavior is photo-retouch oriented:

- keep the face contour almost stable
- move the nose and central face area more than the outline
- avoid full yaw projection that folds the outer face inward
- preserve the current Evoto-like visual result

Live slider preview must keep the same render route at center value `50`.
Do not clear the Turn drag preview at `50`; otherwise the preview falls back for one frame and creates a visible center pop.

## 16. Face Tilt Edge Lock

`Facial Reshape > Head Pose > Tilt` is accepted as a small-angle face-line rotation.

Do not increase the Tilt angle just to match a stronger app sample. The current problem class is edge separation, not insufficient rotation.

The approved Tilt behavior is:

- keep max Tilt at `2.5` degrees
- keep hair and ears outside the Tilt mask
- use a `28 px` outside feather band to attach the face-line edge visually
- keep live preview on the same route at center value `50`; do not clear the drag preview at `50`

## 17. Fallback Rule

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
ignore photo-detected z and use canonical 468 Z
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

## 17. Current Implementation Checkpoint

Current approved checkpoint:

- MediaPipe worker can stay warm in RAM
- FaceShape can reuse cached detected landmarks
- `Up/Dn` uses canonical 468 Z for rigid mask depth
- `Up/Dn` debug overlays are optional and must not block judgment when disabled
- `Up/Dn` applies a rigid 2.5D face mask layer, not a soft relief warp
- the mask includes face-line interior and a small lower-half oval extension
- the mask boundary uses feathered alpha only
- the outside attach band extends to 50px for the current test
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

## 18. Short Version

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
