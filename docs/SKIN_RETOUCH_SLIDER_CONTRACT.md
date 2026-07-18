# Skin Retouch Slider Contract

## Purpose

This document defines the first user-facing `Skin` retouch panel layout.

The `Skin` tab owns general skin-care correction:

- healthier skin tone
- softer skin feel
- pore and fine texture control
- redness control
- shine control

It does not own blemish removal, wrinkle softening, makeup styling, or face-shape edits.

## Source Alignment

Use this document together with:

- `BEAUTY_RETOUCHING_TECHNIQUES.md`
- `BUILD_SKIN_MASK_CONTRACT.md`
- `BUILD_SKIN_SAFE_MASK_CONTRACT.md`
- `TOOLBOX_RETOUCH_PANEL_CONTRACT.md`
- `PERSON_MASK_AND_SUBJECT_FORMULA_SPEC.md`

## UI Layout

The `Skin` tab uses five purpose buttons.
Each button owns exactly three sliders.

```text
Skin

Tone
- Even Tone
- Tone Lift
- Color Cast

Smooth
- Softness
- Texture Protect
- Detail Return

Pores
- Pore Reduce
- Fine Texture
- Edge Protect

Redness
- Red Reduce
- Tone Blend
- Natural Color

Shine
- Shine Reduce
- Highlight Protect
- Texture Return
```

This keeps the tab visually predictable:

```text
5 buttons x 3 sliders = 15 sliders
```

## Slider Meaning

All sliders are unsigned:

```text
0   = no change
100 = maximum approved correction
```

Do not interpret these sliders as `-100..100`.

The first rough image-processing pass is connected.
All 15 sliders use `0` as their neutral default and dispatch the same state snapshot to preview and commit.

## Button Definitions

### Tone

Overall skin tone balancing.

- `Even Tone`: smooth uneven skin tone flow.
- `Tone Lift`: lift dull skin tone without changing identity.
- `Color Cast`: reduce unwanted skin color cast.

### Smooth

General skin softness while avoiding plastic skin.

- `Softness`: soften broad skin texture.
- `Texture Protect`: preserve pores and real skin texture.
- `Detail Return`: restore high-frequency skin detail after smoothing.

### Pores

Pore and fine texture control.

- `Pore Reduce`: reduce visible pore intensity.
- `Fine Texture`: preserve or restore fine skin texture.
- `Edge Protect`: protect facial edges and nearby features from flattening.

### Redness

Localized red and blotchy skin correction without treating it as blemish removal.

- `Red Reduce`: reduce visible redness.
- `Tone Blend`: blend red patches into surrounding skin tone.
- `Natural Color`: keep corrected skin from becoming gray or artificial.

### Shine

Skin shine and oily highlight control.

- `Shine Reduce`: reduce excessive skin shine.
- `Highlight Protect`: keep natural highlight shape and face volume.
- `Texture Return`: restore texture after shine reduction.

## Separation Rules

Skin must stay separate from:

- `Blemish`: acne, spots, moles, freckles, scars, and marks.
- `Wrinkle`: forehead, eye, smile, mouth, chin, and neck wrinkles.
- `Makeup`: base, brow, eye, cheek, and lip styling.
- `Face Shape` / `Face Detail`: shape and landmark-driven structure edits.

## Current Routing

The rough implementation now uses:

- cached MediaPipe 478-point face landmarks
- an inward-feathered face-oval skin mask
- protection regions for eyes, brows, lips, and nostril openings
- a reduced skin-color gate for dark or strongly saturated pixels
- proxy rendering during slider drag
- full-resolution rendering on commit
- one replaceable `Skin` history state while Skin remains the current connected section
- persisted Skin slider state for history undo, redo, and session reload

The five groups have separate rough pixel behavior:

- `Tone`: broad local-tone balance, conservative lift, and color-cast neutralization
- `Smooth`: fine/broad softening with texture protection and detail return
- `Pores`: fine-radius reduction, texture return, and edge protection
- `Redness`: red-excess reduction, local tone blend, and natural-color restoration
- `Shine`: local highlight reduction, highlight protection, and texture return

## Transition Limit

`Skin` and `Blemish` are on the new connected-section route.
They recompose in `Skin -> Blemish` order, and either tab can reset while preserving the other active section.
If a legacy flattened tool commits afterward, the connected result is baked into that image and both live section states are cleared.
This prevents a later tab reset from deleting newer unrelated edits, but a baked connected result cannot yet be removed independently.

Final selected-tab reset semantics require `Wrinkle`, `Hair`, `Makeup`, `Face Shape`, and `Face Detail` to move onto ordered section recomposition.

## Future Mask Routing

Later mask-engine wiring should replace or refine the rough landmark mask with:

- `SkinMask`
- `SkinSafeMask`
- `FaceSkinMask`
- protect masks for eyes, brows, lashes, lips, hair, facial hair, clothing, and accessories

If landmarks, a required mask, or a work area is unavailable, the operation should no-op and avoid writing history.
