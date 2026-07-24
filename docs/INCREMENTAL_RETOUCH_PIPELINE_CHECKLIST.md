# Incremental Retouch Pipeline Checklist

## Status

- Date: 2026-07-24
- State: source implementation checkpoint complete; director verification pending
- Design authority: `INCREMENTAL_RETOUCH_PIPELINE_DESIGN.md`
- UI visual approval belongs to the project director.
- Codex must not take screenshots or snapshots unless explicitly requested.

## How To Use This Checklist

- Complete items in order unless a blocking defect requires an earlier prerequisite.
- Do not tune visual effect strength during the pipeline migration.
- Record failures by stage, state version, input revision, and render quality.
- A build pass is not sufficient. Cross-tab state and pixel output must both pass.
- Keep the current source behavior available until the replacement path passes the relevant section.

## 1. Baseline Audit

- [x] Confirm active right-panel order in `MainWindow.xaml`.
- [x] Confirm Tone uses a photo-selection preview base.
- [x] Confirm Face Shape uses separate mode-specific session bases.
- [x] Confirm Face Detail uses one temporary session base.
- [x] Confirm Skin, Blemish, Wrinkle, Makeup, and Hair share one recomposition path.
- [x] Confirm Background stores pixels but has no persistent adjustment snapshot.
- [x] Confirm editor history persists only the five connected section snapshots.
- [x] Confirm non-connected history capture clears the five live section states.
- [x] Confirm save writes the current bitmap but does not guarantee complete editable-state retention.
- [ ] Record baseline preview timing for each right-panel stage.
- [ ] Record baseline full-resolution commit timing for each right-panel stage.
- [ ] Record baseline peak memory on a representative large portrait.

## 2. Design Lock

- [x] Review and approve the canonical stage order.
- [x] Approve Background as the final raster stage.
- [x] Approve Text as a separate display/save object layer.
- [x] Approve the Phase A flatten-barrier policy for destructive pixel tools.
- [x] Approve the initial Crop boundary policy.
- [ ] Define the preview-cache memory budget.
- [ ] Define the full-resolution cache memory budget.
- [ ] Define acceptable preview latency after baseline measurement.
- [ ] Define acceptable full-resolution commit latency after baseline measurement.

## 3. Pipeline Foundation

- [x] Add a stable `RetouchStageId` for every stage.
- [x] Add `PhotoEditState`.
- [x] Add one complete immutable state snapshot per right-panel stage.
- [x] Add `BaseRevision`.
- [x] Add document-level `StateVersion`.
- [x] Add per-stage `StateRevision`.
- [x] Add per-stage `InputRevision`.
- [x] Add per-stage `OutputRevision`.
- [x] Add `GeometryRevision`.
- [x] Add preview and commit render-quality identifiers.
- [x] Add stage-cache entries.
- [x] Add dirty-range calculation.
- [x] Add render cancellation by photo id and state version.
- [x] Keep the last good preview visible until a newer render succeeds.
- [x] Prevent stale renders from assigning `PhotoItem.Image`.
- [ ] Add diagnostic stage timing without screenshots.

## 4. State Inventory

### Tone

- [x] Persist All-channel curve points.
- [x] Persist Red-channel curve points.
- [x] Persist Green-channel curve points.
- [x] Persist Blue-channel curve points.
- [x] Persist curve strength.
- [x] Persist exposure.
- [x] Persist contrast.
- [x] Persist saturation.
- [x] Persist white balance.
- [x] Persist sharpness.
- [x] Define one complete neutral Tone state.

### Face Shape

- [x] Persist symmetry.
- [x] Persist align or upper-face control.
- [x] Persist cheek.
- [x] Persist bone.
- [x] Persist jaw.
- [x] Persist chin.
- [x] Persist face tilt.
- [x] Persist face turn.
- [x] Persist head tilt.
- [x] Replace separate mode-session bases with one combined snapshot.

### Face Detail

- [x] Promote the existing 41-value snapshot to persistent photo state.
- [x] Restore all 41 values after photo switching.
- [x] Restore all 41 values after undo and redo.
- [x] Restore all 41 values after application restart.
- [x] Remove the temporary session base as the state authority.

### Skin

- [x] Migrate all 15 values without formula changes.
- [x] Preserve neutral-state detection.

### Blemish

- [x] Migrate all 15 values without formula changes.
- [x] Preserve neutral-state detection.

### Wrinkle

- [x] Migrate all 15 values without formula changes.
- [x] Preserve left/right independent values.

### Makeup

- [x] Migrate all 15 values without formula changes.
- [x] Preserve neutral-state detection.

### Hair

- [x] Migrate all 16 values without formula changes.
- [x] Preserve centered neutral values.
- [x] Publish a geometry revision for geometry-changing Hair operations.

### Background

- [x] Persist active mode.
- [x] Persist solid or picked color.
- [x] Persist replacement image identity.
- [x] Persist opacity.
- [x] Persist edge control.
- [x] Persist boundary cleanup.
- [x] Persist edge blur.
- [x] Persist alpha shrink.
- [x] Persist softness.
- [x] Persist alpha gamma.
- [x] Define a neutral Background state.

## 5. Existing Connected-Section Migration

- [x] Wrap Skin renderer as a pipeline stage.
- [x] Wrap Blemish renderer as a pipeline stage.
- [x] Wrap Wrinkle renderer as a pipeline stage.
- [x] Wrap Makeup renderer as a pipeline stage.
- [x] Wrap Hair renderer as a pipeline stage.
- [x] Preserve the current fixed order.
- [x] Preserve current MediaPipe point-map reuse.
- [x] Remove live-state clearing from editor-history capture.
- [x] Replace `GetConnectedRetouchRenderSource()` with upstream cache lookup.
- [x] Replace direct final-image assignment with atomic pipeline publication.
- [x] Keep legacy path available until all five stages pass cross-tab tests.

## 6. Tone Migration

- [x] Route Tone drag preview through the pipeline.
- [x] Route Tone keyboard change through the pipeline.
- [x] Route Tone reset through dirty-range invalidation.
- [x] Stop using the stale photo-selection base as render authority.
- [x] Add Tone history state.
- [x] Restore Tone controls from history.
- [x] Confirm Tone-only changes do not invalidate MediaPipe landmarks.
- [x] Confirm Tone preview and full-resolution commit use the same state.

## 7. Face Shape Migration

- [x] Route every Face Shape mode through one stage state.
- [x] Start each mode from the Tone output cache.
- [x] Recalculate all active Face Shape values as one stage.
- [x] Remove mode-specific session-base authority.
- [ ] Publish transformed landmarks or trigger controlled re-analysis.
- [x] Route Face Shape reset through one neutral stage state.
- [x] Preserve later Face Detail through Background states after Face Shape changes.

## 8. Face Detail Migration

- [x] Start Face Detail from the Face Shape output cache.
- [x] Apply the complete 41-value snapshot on every Face Detail render.
- [x] Remove stale Face Detail session-base reuse.
- [x] Publish geometry changes for downstream masks.
- [ ] Reuse geometry revision for pixel-only Face Detail operations.
- [x] Route Face Detail reset through one neutral stage state.
- [x] Preserve all downstream states after Face Detail changes.

## 9. Background Migration

- [x] Start Background from the latest Hair output cache.
- [x] Reapply stored Background state after any upstream geometry change.
- [ ] Invalidate Background matte after geometry revision changes.
- [x] Reuse Background-only cache when only Background values change.
- [x] Keep white background after Face Shape or Face Detail changes.
- [x] Keep replacement image selection after other tab changes.
- [x] Route Background reset through one neutral stage state.

## 10. Dirty-Range Unit Checks

- [ ] Working Base change dirties Tone through Background.
- [ ] Tone change dirties Tone through Background.
- [ ] Face Shape change dirties Face Shape through Background.
- [ ] Face Detail change dirties Face Detail through Background.
- [ ] Skin change dirties Skin through Background.
- [ ] Blemish change dirties Blemish through Background.
- [ ] Wrinkle change dirties Wrinkle through Background.
- [ ] Makeup change dirties Makeup, Hair, and Background.
- [ ] Hair change dirties Hair and Background.
- [ ] Background change dirties Background only.
- [ ] Inactive downstream stages are skipped.
- [ ] Valid upstream stages are never recalculated.
- [ ] Neutral stages pass through without unnecessary bitmap allocation.

## 11. Cross-Tab State Retention

For every sequence, verify both slider values and visible output.

- [ ] Tone -> Face Shape -> Tone.
- [ ] Tone -> Face Detail -> Tone.
- [ ] Tone -> Skin -> Tone.
- [ ] Tone -> Background -> Tone.
- [ ] Face Shape -> another Face Shape mode -> first Face Shape mode.
- [ ] Face Shape -> Face Detail -> Face Shape.
- [ ] Face Shape -> Background -> Face Shape.
- [ ] Face Detail -> Skin -> Face Detail.
- [ ] Face Detail -> Hair -> Face Detail.
- [ ] Face Detail -> Background -> Face Detail.
- [ ] Skin -> Blemish -> Skin.
- [ ] Skin -> Wrinkle -> Skin.
- [ ] Skin -> Makeup -> Skin.
- [ ] Skin -> Hair -> Skin.
- [ ] Skin -> Background -> Skin.
- [ ] Blemish -> Makeup -> Blemish.
- [ ] Wrinkle -> Hair -> Wrinkle.
- [ ] Makeup -> Background -> Makeup.
- [ ] Hair -> Background -> Hair.
- [ ] Background -> Face Detail -> Background.
- [ ] Background -> Tone -> Background.
- [ ] Complete one full pass through all stages and return to every earlier stage.

## 12. Required Director Scenarios

- [ ] White Background -> Mouth Corner -> white Background remains.
- [ ] Face brightness -> Skin -> brightness and Skin both remain.
- [ ] Face Shape -> Face Detail -> Face Shape readjustment keeps Face Detail.
- [ ] Skin -> Makeup -> Hair -> Skin reset keeps Makeup and Hair.
- [ ] Nose -> Background -> Nose readjustment keeps Background.
- [ ] Hairline -> Background -> Hairline readjustment keeps Background.
- [ ] Makeup -> Tone -> Makeup readjustment keeps Tone.

## 13. Reset Isolation

- [ ] Tone reset removes Tone only.
- [ ] Face Shape reset removes Face Shape only.
- [ ] Face Detail reset removes Face Detail only.
- [ ] Skin reset removes Skin only.
- [ ] Blemish reset removes Blemish only.
- [ ] Wrinkle reset removes Wrinkle only.
- [ ] Makeup reset removes Makeup only.
- [ ] Hair reset removes Hair only.
- [ ] Background reset removes Background only.
- [ ] Reset creates a new history state.
- [ ] Reset never deletes unrelated history entries.
- [ ] Reset never becomes blocked merely because a later pipeline stage is active.

## 14. Undo And Redo

- [x] History capture is side-effect free.
- [x] History stores a complete `PhotoEditStateSnapshot`.
- [ ] Undo restores every right-panel control value.
- [ ] Undo restores the same visible output.
- [ ] Redo restores every right-panel control value.
- [ ] Redo restores the same visible output.
- [ ] Undo after reset restores the removed stage.
- [ ] Redo after reset removes only that stage again.
- [ ] Rapid undo and redo cannot publish stale renders.
- [ ] History trimming keeps a valid base checkpoint.

## 15. Photo Switching And Restart

- [ ] Switch from photo A to photo B without losing A state.
- [ ] Return to photo A and restore every control value.
- [ ] Show the last committed image immediately after photo return.
- [ ] Lazily rebuild transient caches after photo return.
- [x] Persist all stage states to disk.
- [ ] Restart the application and restore all stage values.
- [ ] Restart the application and restore the same committed image.
- [ ] Missing external Background assets produce a clear status.
- [ ] Removing a work-area file removes only that photo's persisted state.

## 16. Preview And Commit Parity

- [ ] Drag preview uses the same stage order as commit.
- [ ] Drag preview uses the same state snapshot as commit.
- [ ] Mouse release commits the latest state version.
- [ ] Keyboard commit uses the latest state version.
- [ ] A cancelled preview cannot overwrite a committed image.
- [ ] A slow earlier render cannot overwrite a fast later render.
- [ ] Preview proxy dimensions map correctly to the final image.
- [ ] Geometry and masks use matching revisions.
- [ ] Latest committed full-resolution output replaces preview atomically.

## 17. Save

- [ ] Save waits for the latest full-resolution state.
- [x] Save never exports a stale preview.
- [ ] Save result matches the latest committed display.
- [ ] Save preserves a white Background after face edits.
- [ ] Save preserves Tone and all active retouch stages.
- [ ] Save composes Text objects once.
- [ ] Save does not alter editable stage state.
- [ ] JPEG and PNG paths use the same final pipeline result.
- [ ] Save after application restart matches save before restart.

## 18. Cache And Performance

- [ ] Cache key includes photo identity.
- [x] Cache key includes render quality.
- [x] Cache key includes input and state revisions.
- [x] Cache key includes geometry or analysis revision where required.
- [ ] Preview cache uses an LRU policy.
- [x] Full-resolution cache uses a bounded LRU policy.
- [x] Deselecting a photo releases preview bitmaps.
- [x] Persistent state survives cache eviction.
- [ ] Cache eviction never changes the visible result.
- [ ] Measure each stage preview time.
- [ ] Measure each stage commit time.
- [ ] Measure full-pass time.
- [ ] Measure earlier-stage edit with downstream replay.
- [ ] Measure Background-only edit.
- [ ] Measure peak memory on a large portrait.
- [ ] Pass the approved preview latency budget.
- [ ] Pass the approved commit latency budget.
- [ ] Pass the approved memory budget.

## 19. Toolbox And Crop Boundaries

### Non-Destructive Tools

- [ ] Hand, Zoom, Ruler, and Sampler leave pipeline state unchanged.
- [ ] Selection tools leave pipeline state unchanged.
- [ ] Path editing leaves pipeline state unchanged.
- [ ] Text editing preserves all raster-stage states.

### Destructive Tools

- [x] Brush uses an explicit operation node or flatten barrier.
- [x] Eraser uses an explicit operation node or flatten barrier.
- [x] Stamp uses an explicit operation node or flatten barrier.
- [x] Healing uses an explicit operation node or flatten barrier.
- [x] Blur and Sharpen use an explicit operation node or flatten barrier.
- [x] Dodge and Burn use an explicit operation node or flatten barrier.
- [x] Liquify uses an explicit operation node or flatten barrier.
- [x] Fill and Gradient use an explicit operation node or flatten barrier.
- [x] No destructive tool silently clears right-panel values.
- [x] Unsafe upstream editing is blocked with a clear reason until replay is supported.

### Crop

- [ ] Crop increments `BaseRevision`.
- [ ] Crop updates dimensions and coordinate mapping.
- [ ] Crop invalidates all dependent caches.
- [ ] Crop never silently clears right-panel values.
- [ ] Unsupported Crop order shows a clear limitation before applying.

## 20. Failure Handling

- [x] Render failure keeps the last good image.
- [x] Render failure keeps every stage state.
- [x] Status identifies the failed stage.
- [x] Failed preview does not create history.
- [x] Failed commit does not replace the final image.
- [x] Failed Background asset load keeps the previous Background.
- [x] Cancellation is not reported as a destructive error.
- [x] Recovery after failure uses the same state version.

## 21. Build And Release Gate

- [x] x64 build passes with zero errors.
- [x] New warnings are reviewed and resolved.
- [x] Application launches and remains responsive.
- [ ] All required director scenarios pass.
- [ ] All reset isolation checks pass.
- [ ] Undo, redo, photo switch, restart, and save checks pass.
- [ ] Performance and memory budgets pass.
- [ ] Project director completes visual approval.
- [x] No screenshots or snapshots are created without an explicit request.
- [ ] Obsolete session-base code is removed only after migration passes.
- [ ] Source and contract documents are synchronized.
- [ ] Final checkpoint is committed.
- [ ] Final checkpoint is pushed.

## Defect Record Template

```text
Sequence:
Photo:
Changed stage:
Expected retained stages:
First incorrect stage:
Preview or Commit:
Input revision:
State version:
Geometry revision:
Visible symptom:
History symptom:
Save symptom:
```
