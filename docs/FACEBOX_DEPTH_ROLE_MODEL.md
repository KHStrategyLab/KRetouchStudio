# FaceBox Depth Role Model

## 1. Purpose

`FaceBox Depth Role Model` defines how KRetouchPro should interpret a frontal `FaceBox` as a lightweight `2.5D facial volume container`.

Its role is:
- treat the face box as more than a flat rectangle
- assign depth meaning to visible face regions
- support side-profile-aware local retouch without building a full 3D head
- give every future `WorkBox` a depth-role starting point

This is not a true depth-map system.
It is a lightweight facial depth-role model.

## 2. Core Rule

The engine should not attempt to solve the full 3D face every time.

Instead:

```text
FaceBox
-> anchor guidance
-> depth role map
-> local SearchROI
-> local WorkBox
-> local detector
-> final mask
-> tool apply
```

Meaning:
- `FaceBox` gives the global stage
- depth roles explain what kind of surface a region is
- local detectors still decide the actual boundary from pixels

## 3. Why This Stays Lightweight

This direction stays light because it does not require:
- dense full-face mesh reconstruction
- full-frame per-pixel depth estimation
- true 3D normal-map solving
- repeated whole-face re-analysis on every slider change

This direction remains lightweight because it uses:
- a single frontal `FaceBox`
- a small set of navigation anchors
- a fixed depth-role interpretation
- local feature work inside bounded ROIs

## 4. FaceBox Is Not A Flat Box

`FaceBox` should be interpreted as a front-facing volume container.

It contains:
- protruding structures
- recessed structures
- transition planes
- outer edge falloff

So the face box is not:
- a flat crop only
- a final contour truth
- a direct warp target by itself

It is:
- the parent container for visible face volume roles

## 5. Depth Role Classes

Every visible face region should first be classified into one of these role types.

### 5.1 `ConvexPeak`

Meaning:
- the highest local protrusion

Examples:
- nose tip
- front chin peak in some faces

Use:
- strongest highlight preservation
- strongest outward depth identity

### 5.2 `ConvexRidge`

Meaning:
- a raised line or elongated protruding surface

Examples:
- nose bridge
- brow ridge impression
- upper lip crest

Use:
- shape guidance
- narrow work box routing

### 5.3 `ConvexSoft`

Meaning:
- rounded soft forward mass, but not the maximum peak

Examples:
- lower lip body
- cheek front mass
- alar body

Use:
- smooth volume edits
- soft mask support

### 5.4 `ConcavePocket`

Meaning:
- an inward recessed local area

Examples:
- eye socket zone
- nostril dark pocket
- tear duct notch
- mouth corner pocket

Use:
- dark-band search
- pocket stabilization
- avoid false outward warp

### 5.5 `TransitionPlane`

Meaning:
- a visible plane connecting one depth role to another

Examples:
- nose side plane
- cheek plane
- philtrum slope
- jaw-to-neck transition

Use:
- broad mask shaping
- smooth falloff routing

### 5.6 `BoundaryFalloff`

Meaning:
- the side or lower edge where the face volume fades toward another ownership region

Examples:
- cheek-to-background edge
- jawline
- lower face into neck
- hairline to forehead

Use:
- contour protection
- matte support
- edge-safe warp rules

## 6. First FaceBox Depth Map By Region

The engine should read the frontal face box using this first stable volume interpretation.

### 6.1 Eye region

- role: `ConcavePocket`
- visible meaning: recessed orbital area
- detector meaning: dark pocket, lid boundary, iris/pupil interior

### 6.2 Nose bridge

- role: `ConvexRidge`
- visible meaning: central raised ridge
- detector meaning: highlight band + side gradient

### 6.3 Nose tip

- role: `ConvexPeak`
- visible meaning: strongest forward surface on the face
- detector meaning: highlight core + nostril relation

### 6.4 Alar / nostril base

- role: `ConvexSoft` + nearby `ConcavePocket`
- visible meaning: rounded side mass with dark cavity
- detector meaning: alar body plus nostril pocket

### 6.5 Upper lip

- role: `ConvexRidge`
- visible meaning: center crest and softer lateral taper
- detector meaning: cupid bow and upper-lip contour

### 6.6 Lower lip

- role: `ConvexSoft`
- visible meaning: rounded front lip mass
- detector meaning: highlight zone and lip body

### 6.7 Mouth slit and corners

- role: `ConcavePocket`
- visible meaning: compression line and inward corner notch
- detector meaning: dark band and endpoint limits

### 6.8 Cheek front

- role: `TransitionPlane` + local `ConvexSoft`
- visible meaning: broad side volume with soft rolloff
- detector meaning: contour plane and shading band

### 6.9 Jawline

- role: `BoundaryFalloff`
- visible meaning: lower face outer contour
- detector meaning: face-to-neck separation band

### 6.10 Under-jaw / double chin

- role: `TransitionPlane` or secondary `ConvexSoft`
- visible meaning: lower soft bulge or shadow band under the jaw
- detector meaning: under-jaw shadow and lower bulge candidate

### 6.11 Chin

- role: `ConvexSoft` or local `ConvexPeak`
- visible meaning: front lower-face support mass
- detector meaning: center anchor and lower-edge band

## 7. Side Profile Coverage Rule

This model exists to support side-profile-aware correction from a frontal image.

The engine should assume:
- not every visible frontal region is equally flat
- protruding zones should resist being treated like background plane
- recessed zones should resist being inflated blindly
- transition planes should carry softer falloff than peaks or pockets

Meaning:
- even from one frontal `FaceBox`, the engine can infer enough structure for practical retouch routing
- the goal is not true side reconstruction
- the goal is side-aware local correction behavior

## 8. WorkBox Inheritance Rule

Every feature `WorkBox` should inherit its first behavior from the parent depth role.

Examples:

### 8.1 `nose_work_box`

- parent role mix:
  - `ConvexRidge`
  - `ConvexPeak`
  - `ConvexSoft`
  - nearby `ConcavePocket`

Meaning:
- bridge, tip, ala, nostril base are all inside the same local task family
- but the final `NoseMask` must still separate them by actual pixel evidence

### 8.2 `eye_work_box`

- parent role mix:
  - `ConcavePocket`
  - surrounding `TransitionPlane`

Meaning:
- do not treat the whole eye box as a bulging surface

### 8.3 `mouth_work_box`

- parent role mix:
  - `ConvexRidge`
  - `ConvexSoft`
  - `ConcavePocket`

Meaning:
- lip body and mouth slit must not share one flat interpretation

### 8.4 `double_chin_work_box`

- parent role mix:
  - `BoundaryFalloff`
  - `TransitionPlane`
  - optional secondary `ConvexSoft`

Meaning:
- the tool must follow jaw-to-neck transition logic, not face-center logic

## 9. Lightweight Policy

The current approved rule is:

```text
No full dense depth map.
No mandatory full-head mesh.
No repeated whole-face 3D solve.
Use FaceBox as a 2.5D role container only.
```

This means:
- depth roles are semantic and structural
- final geometry still comes from local detectors and masks
- heavy 3D should remain optional and later

## 10. Implementation Direction

The practical implementation order should be:

1. keep `FaceBox`
2. keep core anchors
3. assign feature depth role
4. build local `SearchROI`
5. build local `WorkBox`
6. run feature detector
7. resolve final mask
8. apply tool inside the resolved mask

## 11. Short Definition

Short version:

- `FaceBox` is the parent facial volume container
- each region inside it has a depth role
- the role is not the final mask
- the role only guides local detection and local retouch behavior

This is the current lightweight 2.5D depth-role direction for KRetouchPro.
