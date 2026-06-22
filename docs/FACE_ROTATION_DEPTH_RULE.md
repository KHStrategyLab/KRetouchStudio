# Face Rotation Depth Rule

## 1. Purpose

`Face Rotation Depth Rule` defines how future face rotation in KRetouchPro must follow the approved `FaceBox` depth-role model.

Its role is:
- prevent future rotation from collapsing into flat 2D image spin
- keep facial feature ownership tied to local depth roles
- support frontal-image-based side-aware correction without mandatory dense 3D solve
- define the correct rotation order before tool-specific implementation begins

## 2. Core Rule

Future face rotation must not be interpreted as:
- rotate the full face texture as one flat card
- rotate the preview image as a single rectangle
- rotate all landmarks with one uniform image transform

Future face rotation must be interpreted as:

```text
FaceBox
-> depth role assignment
-> feature SearchROI
-> feature WorkBox
-> local detector
-> local mask
-> local rotation or local warp
-> protected composite
```

Meaning:
- `FaceBox` provides the parent facial volume container
- depth roles decide how a region should behave under rotation
- local detectors still decide the true visible boundary from pixels
- final rotation is performed by feature-aware local warp, not flat texture spin

## 3. Why This Stays Lightweight

This rule remains lightweight because it does not require:
- dense full-face mesh reconstruction
- full-frame per-pixel depth estimation
- repeated full-head 3D solve on every edit
- mandatory 106-point contour ownership as final truth

This rule remains practical because it uses:
- one frontal `FaceBox`
- a small approved anchor set
- fixed depth-role classes
- bounded local work boxes
- local pixel-evidence detectors

## 4. Rotation Ownership By Depth Role

Each depth role must react differently when rotation or directional face correction is applied.

### 4.1 `ConvexPeak`

Examples:
- nose tip
- forward chin point in some faces

Rotation rule:
- preserve forward mass identity
- resist flattening into surrounding planes
- keep highlight core and forward contour stable

### 4.2 `ConvexRidge`

Examples:
- nose bridge
- upper lip crest

Rotation rule:
- preserve ridge continuity
- allow directional shift along the ridge axis
- avoid turning ridge zones into flat broad masks

### 4.3 `ConvexSoft`

Examples:
- lower lip body
- alar body
- front cheek mass

Rotation rule:
- allow smooth volume migration
- use soft falloff
- avoid hard contour snapping

### 4.4 `ConcavePocket`

Examples:
- eye socket
- nostril pocket
- mouth corner pocket

Rotation rule:
- preserve inward ownership
- avoid blind outward expansion
- protect dark pocket logic during local warp

### 4.5 `TransitionPlane`

Examples:
- cheek plane
- philtrum slope
- jaw-to-neck transition

Rotation rule:
- carry the softest inter-region falloff
- bridge neighboring roles without tearing
- support directional blending between peaks and pockets

### 4.6 `BoundaryFalloff`

Examples:
- jawline edge
- cheek outer contour
- lower face into neck

Rotation rule:
- protect subject boundary
- prevent background drag
- maintain matte-safe outer contour behavior

## 5. Feature Rotation Rules

### 5.1 Nose

The nose must be treated as the primary front-facing depth reference.

Role mix:
- `ConvexRidge`
- `ConvexPeak`
- `ConvexSoft`
- nearby `ConcavePocket`

Meaning:
- bridge, tip, alar body, and nostril pocket must not rotate as one flat block
- local nose correction should follow the approved `nose_work_box`
- the final nose mask must still come from local pixel evidence

### 5.2 Eyes

The eye region must be treated as a recessed pocket system, not a protruding plane.

Role mix:
- `ConcavePocket`
- surrounding `TransitionPlane`

Meaning:
- eye correction must preserve socket behavior
- eyelid, iris, pupil, and corner routing must remain local
- future face rotation must not drag the orbital zone like a cheek plane

### 5.3 Mouth

The mouth must be treated as a mixed ridge-soft-pocket system.

Role mix:
- `ConvexRidge`
- `ConvexSoft`
- `ConcavePocket`

Meaning:
- upper lip, lower lip, mouth slit, and corners must remain structurally distinct
- mouth rotation must not reduce the whole mouth box to one flat band

### 5.4 Jawline And Under-Jaw

The jawline is not face-center volume.
It is a contour and ownership boundary.

Role mix:
- `BoundaryFalloff`
- `TransitionPlane`
- optional secondary `ConvexSoft`

Meaning:
- jaw and neck must be separated before rotation-style correction
- under-jaw shading and double-chin tools must follow boundary logic first

## 6. Protected Rotation Composite Rule

Any future face rotation flow must preserve local ownership before final compositing.

Required order:

```text
OriginalImage
-> FaceBox
-> depth role guidance
-> feature SearchROI
-> feature WorkBox
-> local detector
-> resolved local mask
-> local rotation warp
-> protected composite
-> MainPreview or Original Apply
```

Meaning:
- do not rotate from preview output
- do not rotate from already color-shifted or effect-stacked buffers
- do not treat the last visible image as the next detector input

## 7. Side-Aware Interpretation Rule

This rule exists so frontal input can support side-aware correction behavior.

The goal is not:
- true full side-face reconstruction
- true volumetric head simulation

The goal is:
- infer which regions are forward, recessed, transitional, or boundary-based
- let each feature respond correctly when directional correction is requested

This is enough to support:
- face-shape directional correction
- nose direction correction
- jawline asymmetry correction
- local turn-aware retouch behavior

## 8. Forbidden Simplifications

The engine must not:
- rotate the whole face as one flat image card
- use one uniform transform for all facial regions
- merge eye, nose, mouth, and jaw behavior into one common warp rule
- ignore jaw-to-neck boundary separation
- ignore concave pocket logic in the eye and nostril regions

## 9. Implementation Direction

Future implementation should follow this order:

1. keep `FaceBox`
2. assign depth roles from the `FaceBox` model
3. derive feature `SearchROI`
4. derive feature `WorkBox`
5. run local detector
6. resolve final feature mask
7. apply local rotation or directional warp
8. composite back with protected ownership

## 10. Short Definition

Short version:

- future face rotation follows depth roles, not flat 2D image rotation
- `FaceBox` remains the parent container
- local detectors remain the final boundary owner
- rotation is feature-local, role-guided, and lightweight

## 11. Phase 1 Test Surface Checkpoint

The first visual validation surface is approved as a test overlay, not as final pixel warping.

Implemented validation behavior:

- `3D Test Surface` toggles a preview overlay.
- `Yaw`, `Pitch`, and `Roll` sliders update the projected mesh in real time.
- `Reset` returns the pose to zero rotation.
- zero-rotation drift is displayed as a quick alignment check.
- major triangle edges and projected vertices are drawn over the main preview.

Current accepted result:

- UI surface, slider routing, reset behavior, and overlay projection are accepted.
- zero-pose projection reported `Drift 0.00px` during user validation.
- philtrum subdivision removed the direct `nose_tip -> mouth_*` fan collapse pattern.

Current limitation:

- this is still a coarse validation mesh.
- full face texture warping is not connected yet.
- future refinement should increase mesh density around nose base, philtrum, upper lip, cheeks, and jaw before final image warp.

## 12. Philtrum Subdivision Rule

The nose tip must not connect directly to the mouth fan.

Required topology path:

```text
nose_tip
-> septum_base_support
-> philtrum_center
-> mouth_* anchors
```

Meaning:

- the septum and philtrum region must distribute texture tension before reaching the mouth.
- nose tip rotation must not pull the upper lip texture into one convergence point.
- this strip is the minimum accepted topology for future face-rotation preview and warp work.
