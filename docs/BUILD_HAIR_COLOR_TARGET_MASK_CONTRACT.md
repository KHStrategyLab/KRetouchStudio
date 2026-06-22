# BuildHairColorTargetMask Contract

## 1. Purpose

`BuildHairColorTargetMask(...)` defines the first stable hair-color target mask contract for KRetouchPro.

Its role is:
- narrow broad scalp-hair ownership into a safer hair-color execution mask
- keep face skin, ears, eyebrows, eyelashes, facial hair, and accessories out of hair color work
- provide the operational apply mask for hair-only color tools

This function does not build the whole hair preserve mask from scratch.
It derives a color-safe target from existing hair ownership.

## 2. Current Stage Definition

At the current project stage:

- `HairMask` means scalp-hair preserve ownership
- hair color work requires a narrower target than broad preserve ownership
- eye-area hair, facial hair, and accessories must stay out

Therefore `BuildHairColorTargetMask(...)` is defined as:

**the mask-construction step that turns `HairMask` into a safer hair-color apply mask by subtracting nearby protected regions**

It is not:
- a full hair detector
- an eyebrow color mask
- a beard recolor mask
- a background compositing mask

## 3. Function Name

Preferred name:

`BuildHairColorTargetMask(...)`

Related later functions:

- `BuildHairMask(...)`
- `ApplyHairColor(...)`
- `BuildPersonMask(...)`
- `BuildSubjectMatte(...)`

## 4. Ownership

Pillar:

- Mask Construction

This function should construct an operational hair-color target only.
It must not directly:
- recolor hair
- redefine scalp-hair ownership
- redefine face skin or ear ownership
- replace the background

## 5. Primary Use Cases

### 5.1 Hair color apply mask

Use `HairColorTargetMask` as the final safe apply mask for:

- hair recolor
- hair tint
- selective hair tone correction

### 5.2 Protect-side routing

Use `HairColorTargetMask` to keep color work out of:

- face skin
- ear skin
- eyebrow
- eyelash
- facial hair
- accessories

### 5.3 Stable preview execution

Use `HairColorTargetMask` so preview and export hair color remain consistent and do not spill into nearby face structures.

## 6. Input Contract

The builder should accept the following inputs.

### 6.1 Required

- `HairMask`
- `FaceSkinMask`
- `EarMask`
- `EyebrowMask`
- `EyelashMask`
- `FacialHairMask`
- `AccessoryMask`
- `ImageWidth`
- `ImageHeight`

### 6.2 Recommended

- `HairAlpha`
- `FaceRegionMask`
- `HairlineGuide`
- `HeadROI`

### 6.3 Optional

- `BuildMode`
  - `HairColor`
  - `DebugOnly`
- `ColorTargetEdgePolicy`
- `ColorProtectionRadius`

## 7. Core Meaning Rule

`HairColorTargetMask` means the safe execution mask for scalp-hair color work.

It should include:
- scalp hair that should actually receive color
- head-hair silhouette interior that remains after protected-region subtraction

It should exclude:
- face skin
- ears
- eyebrows
- eyelashes
- facial hair
- accessories
- background

## 8. Formula Rule

The formula-spec operational definition already fixed in the project is:

\[
HairColorTargetMask =
HairMask
\cdot (1 - FaceSkinMask)
\cdot (1 - EarMask)
\cdot (1 - EyebrowMask)
\cdot (1 - EyelashMask)
\cdot (1 - FacialHairMask)
\cdot (1 - AccessoryMask)
\]

This keeps color work narrower than general `HairMask`.

## 9. Separation Rule

`HairColorTargetMask` must stay separate from:

- `FaceSkinMask`
- `EarMask`
- `EyebrowMask`
- `EyelashMask`
- `FacialHairMask`
- `AccessoryMask`

Important meaning split:

- `HairMask` = preserve-side scalp hair ownership
- `HairColorTargetMask` = operationally safe color target

Do not silently treat all preserved hair as automatically color-editable.

## 10. Output Contract

Preferred result structure:

`HairColorTargetMaskBuildResult`

Suggested fields:

- `Status`
- `Confidence`
- `HairColorTargetMask`
- `Warnings`

Minimum required output:

- `HairColorTargetMask`

## 11. Downstream Use Rule

Use `HairColorTargetMask` for:
- hair recolor apply routing
- hair tint preview
- color-safe export rendering

Do not use `HairColorTargetMask` as:
- full `HairMask`
- beard recolor target
- eyebrow recolor target
- person or background mask

## 12. Debug Outputs

Recommended debug outputs:

- `debug_hair_color_target_mask.png`
- `debug_hair_color_target_mask_overlay.png`
- `debug_hair_color_target_mask_report.json`

Recommended report fields:

- build status
- confidence
- protected regions used
- target shrink amount
- warnings

## 13. Do-Not-Do Rules

- Do not let face skin receive hair color.
- Do not let ears receive hair color.
- Do not let eyebrow or eyelash receive hair color.
- Do not let beard or mustache receive scalp-hair color by default.
- Do not let accessories be recolored as hair.

## 14. Success Condition

`BuildHairColorTargetMask(...)` is successful when:

- scalp-hair color work stays inside true head-hair targets
- nearby protected face regions remain untouched
- the operational color mask is safer than raw `HairMask`

## 15. One-Line Definition

`BuildHairColorTargetMask(...)` constructs the safe operational hair-color mask by subtracting face skin, ears, eye-area hair, facial hair, and accessories from `HairMask`, so that hair color work stays on actual scalp hair.
