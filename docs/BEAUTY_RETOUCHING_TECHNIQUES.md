# Beauty Retouching Techniques

This document captures practical studio retouching rules that can later become tool behavior in KRetouchStudio.

This is a technique guide, not a runtime contract. Implementation must still follow the detection, mask, preview, and history contracts owned by the related tool documents.

## 1. Skin Texture And Smoothing

The skin tool must avoid a flat plastic blur.

Preferred direction:

- Split skin correction into tone smoothing and texture preservation.
- Treat low frequency as color, redness, uneven tone, and broad blotch cleanup.
- Treat high frequency as pores, tiny skin texture, and fine facial hair.
- Re-apply preserved texture lightly after tone correction.
- A first macro target can use a skin mask, soft low-frequency smoothing, and a light texture return layer around `30%`.

Working rule:

- Smooth color, not identity.
- Preserve pores unless the user explicitly chooses a stronger beauty style.

## 2. Wrinkle Handling

Wrinkles should usually be softened, not erased.

Preferred direction:

- Correct the dark shadow component of wrinkles toward nearby skin tone.
- Keep some depth so the face does not become artificial.
- Default opacity target for wrinkle reduction: `40-60%`.

Working rule:

- Remove distraction.
- Preserve natural age and facial volume.

## 3. Blemish, Acne, And Spot Removal

Small blemishes are better handled by local skin replacement than broad smoothing.

Preferred direction:

- Use a small local patch search around the blemish.
- Prefer nearby skin with low variance and similar tone.
- Blend the patch like a healing brush, not a hard clone.
- Keep local lighting direction and skin texture continuity.

Mole preservation rule:

- Do not automatically remove strong round dark-brown marks that may be intentional beauty marks.
- A future automatic remover should exclude likely moles by contour clarity, darkness, size, and isolation.
- User confirmation should be required for ambiguous beauty marks.

## 4. Stray Hair Cleanup

Stray hair cleanup must preserve hair realism.

Preferred direction:

- Detect thin line-like pixels that differ from local background or skin.
- Follow the strand direction until the line fades or exits the target area.
- For strands crossing the forehead or cutting into the face contour, remove more aggressively.
- For strands outside the hair silhouette, fade only the tip or reduce alpha so the outline remains natural.

Working rule:

- Remove distracting face-crossing hairs.
- Do not make the outer hairstyle edge look cut out.

## 5. Tattoo And Large Scar Removal

Large marks need model-based skin reconstruction rather than spot healing.

Preferred direction:

- Build or reuse a `SkinColorModel` from surrounding skin.
- Estimate the local light and gradient direction.
- Fill the marked area with reconstructed skin tone and gradient.
- Add subtle artificial texture or uniform noise around `1-2%` to avoid a pasted flat region.

Working rule:

- Reconstruct skin tone, gradient, and texture together.
- Do not rely on a small healing brush for large high-contrast regions.

## 6. Hair Shine

Hair beauty work should use contrast and directional texture instead of broad blur.

Preferred direction:

- Use the hair mask.
- Brighten existing highlight bands with a light dodge-style pass.
- Deepen shadow strands with a light burn-style pass.
- Preserve strand direction.
- A weak high-pass texture layer can increase perceived shine and hair detail.

Working rule:

- Enhance existing shine.
- Do not paint generic highlight stripes over unrelated hair flow.

## 7. Frizz Taming

Frizz cleanup should calm surface noise while preserving large hair mass.

Preferred direction:

- Work inside a hair mask.
- Use a weak surface blur or median-style cleanup.
- Protect major strand groups and silhouette edges.
- Preserve the hair shape before smoothing fine surface disorder.

Working rule:

- Calm surface texture.
- Keep hair mass, direction, and edge realism.

## 8. Hair Volume

Hair volume warps must pin the face before moving outer hair pixels.

Planning references:

- Forehead / hairline pin: MediaPipe `10`.
- Cheekbone safety pins: MediaPipe `234`, `454`.
- Upper temple / side hair routing references: MediaPipe `109`, `338`.

Preferred direction:

- For top volume, keep the forehead line pinned and lift only outer top hair pixels.
- For side volume, keep cheekbone and face contour stable while expanding upper temple hair outward.
- Treat these points as planning anchors, not final hair-mask boundaries.

Working rule:

- Pin face and forehead first.
- Move hair, not the facial structure.

## 9. Hairline Correction

Hairline correction is a high-risk identity edit and should be conservative.

Preferred direction:

- Use upper face and hairline references such as `10`, `109`, and `338`.
- Confirm forehead, brow, and hair mask separation before pulling the hairline.
- Pull only the hairline / hair pixels downward.
- Do not compress brows, eyes, or facial skin unless explicitly designed as a separate face-shape edit.

Working rule:

- Reduce forehead area by moving hairline pixels only after face safety is confirmed.

## 10. Scalp Covering

Scalp covering should combine tone correction and texture transfer.

Preferred direction:

- Darken bright scalp gaps with a controlled color-burn-like pass.
- Copy nearby dense hair texture along the same strand direction.
- Match brush angle and flow to the local hair direction.
- Avoid repeating obvious cloned patterns.

Working rule:

- First reduce bright skin visibility.
- Then restore believable strand texture.

## 11. Implementation Routing

Before turning any technique into code:

1. Identify the target region with the detector/mask pipeline.
2. Separate protected structures from editable pixels.
3. Use local proxy preview for heavy or local edits.
4. Keep preview behavior separate from commit, save, and history.
5. Let the project director judge visual quality in the app, not from screenshots or snapshots.

Related documents:

- `FACE_MEDIAPIPE_LANDMARK_INDEX_GUIDE.md`
- `DETECTION_PIPELINE_CONTRACT.md`
- `PERSON_MASK_AND_SUBJECT_FORMULA_SPEC.md`
- `LOCAL_PROXY_WORKBENCH_DESIGN.md`
