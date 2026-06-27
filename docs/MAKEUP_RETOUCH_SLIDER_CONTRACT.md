# Makeup Retouch Slider Contract

## Purpose

This document defines the first user-facing `Makeup` retouch panel layout.

The `Makeup` tab owns cosmetic styling controls:

- base makeup
- brow makeup
- eye makeup
- cheek color and dimension
- lip color and finish

It does not own skin cleanup, blemish removal, wrinkle softening, facial landmark reshaping, or structural lip/eye/brow edits.

## Source Alignment

Use this document together with:

- `CORE_FORMULAS.md`
- `BEAUTY_RETOUCHING_TECHNIQUES.md`
- `PERSON_MASK_AND_SUBJECT_FORMULA_SPEC.md`
- `FACE_DETAIL_RETOUCH_SLIDER_CONTRACT.md`
- `TOOLBOX_RETOUCH_PANEL_CONTRACT.md`

## UI Layout

The `Makeup` tab uses five purpose buttons.
Each button owns exactly three sliders.

```text
Makeup

Base
- Coverage
- Evenness
- Finish

Brow
- Density
- Shape
- Color

Eye
- Shadow
- Liner
- Lash

Cheek
- Blush
- Contour
- Highlight

Lip
- Color
- Saturation
- Gloss
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

### Base

Foundation and base makeup appearance.

- `Coverage`: base makeup opacity and coverage strength.
- `Evenness`: smooth base tone continuity.
- `Finish`: balance final surface finish without splitting the UI into separate matte and glow controls.

### Brow

Eyebrow makeup styling.

- `Density`: increase or balance brow fill density.
- `Shape`: clean and guide brow silhouette.
- `Color`: adjust brow color depth.

### Eye

Eye makeup styling.

- `Shadow`: apply or strengthen eye shadow.
- `Liner`: define eyeliner strength.
- `Lash`: enhance eyelash visibility.

### Cheek

Cheek color and dimension styling.

- `Blush`: apply cheek color.
- `Contour`: add or balance contour depth.
- `Highlight`: add cheek highlight.

### Lip

Lip makeup color and finish.

- `Color`: apply or shift lip color.
- `Saturation`: adjust lip color intensity.
- `Gloss`: control lip gloss and shine.

## Separation Rules

Makeup must stay separate from:

- `Skin`: broad tone, softness, pore, redness, and shine control.
- `Blemish`: acne, spots, moles, freckles, scars, and marks.
- `Wrinkle`: line and fold softening by face or neck location.
- `Face Detail`: structural eye, brow, mouth, and neck edits.
- `Face Shape`: landmark-driven facial reshape operations.

The following controls are intentionally excluded from the first makeup layout:

- `Brow Tail`: covered by `Brow Shape` or later detailed brow tooling.
- `Eye Bright`: belongs to eye retouch/detail, not makeup styling.
- `Cheek Softness`: belongs to skin or blend execution, not a user-facing makeup purpose.
- `Lip Shape`: belongs to mouth/face detail shape editing.

## Future Routing

Later engine wiring should resolve:

- `FaceSkinMask`
- `EyebrowMask`
- `EyelidMask`
- `EyeMakeupMask`
- `EyelashMask`
- `CheekMask`
- `LipMask`
- protect masks for eyes, teeth, facial hair, hair, clothing, and accessories

If a required mask or work area is unavailable, the operation should no-op and avoid writing history.
