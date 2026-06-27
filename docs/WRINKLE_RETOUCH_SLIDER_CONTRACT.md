# Wrinkle Retouch Slider Contract

## Purpose

This document defines the first user-facing `Wrinkle` retouch panel layout.

The goal is not to erase age or expression.
The goal is to soften distracting wrinkle depth while preserving natural face volume.

## Source Alignment

Use this document together with:

- `BEAUTY_RETOUCHING_TECHNIQUES.md`
- `PERSON_MASK_AND_SUBJECT_FORMULA_SPEC.md`
- `BUILD_NECK_WRINKLE_MASK_CONTRACT.md`
- `TOOLBOX_RETOUCH_PANEL_CONTRACT.md`
- `FACE_DETAIL_RETOUCH_SLIDER_CONTRACT.md`

## UI Layout

The `Wrinkle` tab must not use mode buttons.

Wrinkles are chosen by visible face location, so the UI should show all main wrinkle locations from top to bottom:

```text
Wrinkle

Forehead        [ slider ]
Frown           [ slider ]
Crow's Feet     L [ slider ] [link] R [ slider ]
Under Eye       L [ slider ] [link] R [ slider ]
Bunny           L [ slider ] [link] R [ slider ]
Smile Fold      L [ slider ] [link] R [ slider ]
Marionette      L [ slider ] [link] R [ slider ]
Lip Lines       [ slider ]
Chin Crease     [ slider ]
Neck            [ slider ]
```

Do not group these rows under visible `Upper`, `Middle`, `Lower`, or `Neck` section labels.
The top-to-bottom body order is enough for the user.

## Slider Meaning

All sliders are unsigned:

```text
0   = no change
100 = maximum approved wrinkle softening
```

Do not interpret these sliders as `-100..100`.

The first UI pass is parameter-only.
Image processing can be connected later through documented mask and work-area routing.

## Single Sliders

### Forehead

Horizontal forehead lines across the upper forehead.

OperationId:

```text
ForeheadWrinkleSoften
```

### Frown

Central glabellar / "11" lines between the brows.

OperationId:

```text
FrownWrinkleSoften
```

### Lip Lines

Vertical fine lines around the upper lip and lip border.

OperationId:

```text
LipLineSoften
```

### Chin Crease

Central chin crease and orange-peel-like chin texture.

OperationId:

```text
ChinCreaseSoften
```

### Neck

Combined neck wrinkle control.
This includes horizontal neck lines and overall vertical neck band appearance in one rough-pass slider.

OperationId:

```text
NeckWrinkleSoften
```

## Linked Pair Sliders

Linked pair sliders default to linked mode.
When linked, left and right values move together.
When unlinked, either side can be adjusted independently.

The engine must still receive explicit left and right values.

### Crow's Feet

Outer-eye fine wrinkles on the left and right sides.

OperationIds:

```text
LeftCrowsFeetSoften
RightCrowsFeetSoften
```

### Under Eye

Fine wrinkle and crease softening below each eye.

OperationIds:

```text
LeftUnderEyeWrinkleSoften
RightUnderEyeWrinkleSoften
```

### Bunny

Nose bridge and side-nose wrinkle lines from squinting or scrunching.

OperationIds:

```text
LeftBunnyLineSoften
RightBunnyLineSoften
```

### Smile Fold

Nasolabial fold from the side of the nose toward the mouth corner.

OperationIds:

```text
LeftSmileFoldSoften
RightSmileFoldSoften
```

### Marionette

Lines from the mouth corners down toward the chin.

OperationIds:

```text
LeftMarionetteLineSoften
RightMarionetteLineSoften
```

## Processing Rule

Wrinkle work must soften, not erase.

Follow these limits:

- preserve natural face volume
- preserve pores and nearby skin texture
- protect eyes, lips, brows, hair, facial hair, clothing, and accessories
- avoid flattening nasolabial and marionette folds completely
- keep neck wrinkle work separate from double-chin and jawline shape edits

## Future Routing

Later engine wiring should resolve:

- `WrinkleMask`
- `FaceSkinMask`
- `SkinSafeMask`
- `NeckWrinkleMask`
- left/right local work boxes for pair rows
- protect masks for eyes, lips, brows, hair, facial hair, clothing, and accessories

If a required mask or work box is unavailable, the operation should no-op and avoid writing history.
