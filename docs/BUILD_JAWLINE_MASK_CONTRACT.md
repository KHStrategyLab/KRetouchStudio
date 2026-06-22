# BuildJawlineMask Contract

## 1. Purpose

`BuildJawlineMask(...)` defines the first stable jawline boundary-mask contract for KRetouchPro.

Its role is:
- isolate the lower-face boundary between face and neck
- provide a geometric reference line for jawline support and lower-face work
- keep jawline meaning separate from full neck skin and double-chin feature regions

This function is not a full lower-face skin mask builder.
It constructs the boundary structure used by later feature and warp tools.

## 2. Current Stage Definition

At the current project stage:

- lower-face support is one of the active weak areas
- `JawlineMask` is needed as a reference boundary, not as a full filled region
- double-chin and neck work depend on a stable jawline divider

Therefore `BuildJawlineMask(...)` is defined as:

**the mask-construction step that derives the lower-face boundary structure between `FaceRegionMask` and `NeckBioMask`**

It is not:
- a face-skin mask builder
- a neck-skin mask builder
- a double-chin mask builder
- a direct warp operation

## 3. Function Name

Preferred name:

`BuildJawlineMask(...)`

Related later functions:

- `BuildFaceSkinMask(...)`
- `BuildDoubleChinMask(...)`
- `BuildNeckWrinkleMask(...)`
- `BuildDoubleChinWorkArea(...)`
- `WarpJawline(...)`

## 4. Ownership

Pillar:

- Mask Construction

This function should construct a boundary/reference mask only.
It must not directly:
- smooth texture
- recolor skin
- replace the background
- widen into full neck or face regions

## 5. Primary Use Cases

### 5.1 Jawline enhancement

Use `JawlineMask` as the target boundary for jawline support and contour correction.

### 5.2 Double-chin separation

Use `JawlineMask` as the upper divider for:

- `DoubleChinMask`
- `SubmentalRegionMask`

### 5.3 Lower-face geometry routing

Use `JawlineMask` as a reference line for:

- lower-face warp
- jaw-to-neck transition support
- under-jaw local work areas

## 6. Input Contract

The builder should accept the following inputs.

### 6.1 Required

- `FaceRegionMask`
- `NeckBioMask`
- `JawlineProb`
- `ImageWidth`
- `ImageHeight`

### 6.2 Recommended

- `FaceSkinMask`
- `UnderJawSkinMask`
- `ChinAnchor`
- `JawAnchorGuide`
- `NeckBaseLine`

### 6.3 Optional

- `BuildMode`
  - `ReferenceOnly`
  - `WarpSupport`
  - `DebugOnly`
- `BoundaryThickness`
- `ConfidenceMap`

## 7. Core Meaning Rule

`JawlineMask` means the lower-face outer boundary structure.

It should include:
- the face-to-neck transition line under the cheeks and chin
- the lower contour that defines the face ending before upper neck takeover

It should exclude:
- full face skin region
- full neck skin region
- double-chin fill region
- clothing boundary
- beard volume as a filled area by default

## 8. Formula Rule

The formula-spec definition already fixed in the project is:

\[
JawlineMask =
Boundary(FaceRegionMask,\ NeckBioMask)
\cdot JawlineProb
\]

This means `JawlineMask` is a boundary structure, not a full region fill.

## 9. Separation Rule

`JawlineMask` must stay separate from:

- `FaceSkinMask`
- `NeckSkinMask`
- `DoubleChinMask`
- `NeckWrinkleMask`
- `ClothingMask`

Important meaning split:

- `JawlineMask` = boundary/reference structure
- `DoubleChinMask` = under-jaw feature region
- `NeckWrinkleMask` = texture/status feature inside neck skin

Do not widen `JawlineMask` into a full under-jaw area.

## 10. Output Contract

Preferred result structure:

`JawlineMaskBuildResult`

Suggested fields:

- `Status`
- `Confidence`
- `JawlineMask`
- `Warnings`

Minimum required output:

- `JawlineMask`

## 11. Downstream Use Rule

Use `JawlineMask` for:
- jawline enhancement support
- lower-face work-area routing
- upper divider for double-chin work

Do not use `JawlineMask` as:
- full `FaceSkinMask`
- full `NeckSkinMask`
- a final person boundary by itself

## 12. Debug Outputs

Recommended debug outputs:

- `debug_jawline_mask.png`
- `debug_jawline_mask_overlay.png`
- `debug_jawline_mask_report.json`

Recommended report fields:

- build status
- confidence
- boundary thickness
- source masks used
- warnings

## 13. Do-Not-Do Rules

- Do not treat jawline as a filled lower-face skin region.
- Do not merge neck wrinkles into `JawlineMask`.
- Do not use clothing neckline as the jawline substitute.
- Do not force beard density to become jawline boundary certainty.

## 14. Success Condition

`BuildJawlineMask(...)` is successful when:

- the face-to-neck boundary becomes stable enough for lower-face routing
- it remains a boundary/reference structure
- double-chin and neck work can use it without semantic overlap

## 15. One-Line Definition

`BuildJawlineMask(...)` constructs the lower-face boundary mask between `FaceRegionMask` and `NeckBioMask` so that jawline support, under-jaw routing, and lower-face geometry work can use a stable face-to-neck divider.
