# Blemish Retouch Slider Contract

## Purpose

This document defines the first user-facing `Blemish` retouch panel layout.

The `Blemish` tab owns visible skin mark cleanup:

- acne and raised red marks
- small spots and pigmentation marks
- moles and beauty marks
- freckles
- scars and healed marks

It does not own general skin smoothing, wrinkle softening, makeup styling, face-shape edits, or brush size selection.

## Source Alignment

Use this document together with:

- `BEAUTY_RETOUCHING_TECHNIQUES.md`
- `PERSON_MASK_AND_SUBJECT_FORMULA_SPEC.md`
- `TOOLBOX_RETOUCH_PANEL_CONTRACT.md`
- `BUILD_SKIN_MASK_CONTRACT.md`
- `BUILD_SKIN_SAFE_MASK_CONTRACT.md`

## UI Layout

The `Blemish` tab uses five purpose buttons.
Each button owns exactly three sliders.

```text
Blemish

Acne
- Reduce
- Redness
- Bump

Spot
- Remove
- Blend
- Texture Match

Mole
- Reduce
- Protect
- Edge Blend

Freckle
- Fade
- Density
- Protect

Scar
- Soften
- Tone Blend
- Texture Match
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

The first UI pass is parameter-only.
Image processing can be connected later through documented mask and work-area routing.

## Button Definitions

### Acne

Active acne and raised red mark control.

- `Reduce`: reduce visible acne intensity.
- `Redness`: reduce acne redness without flattening surrounding skin tone.
- `Bump`: soften raised bump appearance.

### Spot

Small flat mark and pigmentation cleanup.

- `Remove`: reduce or remove visible spot evidence.
- `Blend`: blend the corrected area into surrounding skin.
- `Texture Match`: restore local skin texture so the corrected area does not look patched.

### Mole

Mole and beauty mark handling.

- `Reduce`: reduce mole intensity when requested.
- `Protect`: preserve intentional moles and beauty marks.
- `Edge Blend`: avoid hard correction edges around the mole boundary.

### Freckle

Freckle density and preservation control.

- `Fade`: soften freckle visibility.
- `Density`: control how many freckles remain visible.
- `Protect`: preserve natural freckle character when the correction should stay subtle.

### Scar

Scar and healed mark softening.

- `Soften`: soften visible scar contrast and texture.
- `Tone Blend`: blend scar color into surrounding skin tone.
- `Texture Match`: restore local texture continuity.

## Separation Rules

Blemish must stay separate from:

- `Skin`: broad tone, softness, pore, redness, and shine control.
- `Wrinkle`: line and fold softening by face or neck location.
- `Makeup`: color and styling overlays.
- `Face Shape` / `Face Detail`: landmark-driven structure edits.

`Size` is not a Blemish panel slider.
Target size belongs to selection, brush, work-area, or local proxy tooling.

## Future Routing

Later engine wiring should resolve:

- `SkinBlemishMask`
- `BlemishSpotMask`
- `AcneMask`
- `MoleMask`
- `FreckleMask`
- `ScarMask`
- `SkinSafeMask`
- `FaceSkinMask`

If a required mask or work area is unavailable, the operation should no-op and avoid writing history.
