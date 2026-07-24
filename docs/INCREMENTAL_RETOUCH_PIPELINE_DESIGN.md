# Incremental Retouch Pipeline Design

## Status

- Date: 2026-07-24
- State: design plan before implementation
- Source code remains the runtime authority until this migration is complete.
- This document defines state retention and incremental recomposition. It does not tune visual effect quality.

## Purpose

Every active retouch value must remain attached to its photo while the user moves between tabs, tools, history states, photos, save operations, and application sessions.

The renderer must preserve earlier work without recalculating every stage from the original image on every slider movement.

The required model is:

```text
Persistent edit state
+ stage output caches
+ dirty-range invalidation
+ partial downstream recomposition
```

## Current Source Findings

The current application has two different editing models.

### Connected Section Model

These sections already share a fixed recomposition path:

```text
Skin -> Blemish -> Wrinkle -> Makeup -> Hair
```

Their snapshots are captured in editor history and restored after undo, redo, photo switching, and application restart.

### Flattened Or Session-Base Model

These areas are not part of the same persistent state basket:

- Tone Correction
- Face Shape
- Face Detail
- Background
- destructive toolbox operations
- Crop

Their output is mainly retained as pixels or temporary session bases.

Current failure modes include:

- Tone Correction rendering from a stale photo-selection base.
- Face Shape keeping a separate base image for each mode.
- Face Detail keeping one temporary base that becomes stale after another operation.
- Background retaining pixels but not a reusable background state snapshot.
- any non-connected history capture clearing the live Skin, Blemish, Wrinkle, Makeup, and Hair states.
- only five retouch section snapshots being persisted in editor history.

## Goals

1. Preserve every right-panel value per photo.
2. Keep the last valid output of each stage.
3. Recalculate only the changed stage and its downstream dependents.
4. Never silently clear another tab's values.
5. Make preview, commit, history, save, and restored-session output agree.
6. Keep tab reset isolated to the selected tab.
7. Keep the UI responsive during drag preview.
8. Bound memory use for large images.
9. Make destructive toolbox boundaries explicit until they support replay.

## Non-Goals

- Final mask quality or effect-strength tuning.
- New retouch tabs or new beauty functions.
- A broad UI redesign.
- Immediate command replay for every existing pixel tool in the first migration phase.
- Persisting bitmap caches to disk. Persistent state is required; caches may be rebuilt.

## Core Terms

### Working Base

The raster input accepted by the incremental retouch pipeline.

It has a `BaseRevision` that changes when an approved base-changing operation occurs.

### Stage State

The complete reusable values for one pipeline stage.

Examples:

- all Tone curve and quick-control values
- all nine Face Shape mode values
- all 41 Face Detail values
- all 15 Skin values
- the active Background mode, replacement resource, and seven adjustment values

### Stage Cache

The latest valid output produced by a stage for a specific input revision and state revision.

### Dirty Range

The changed stage and every downstream stage whose cached output is no longer valid.

### Render Quality

- `Preview`: bounded long side, currently 1200 pixels where supported
- `Commit`: original image dimensions

### Flatten Barrier

A destructive operation that cannot yet be replayed from stored parameters.

Flatten barriers must be visible in state and history. They must never silently clear adjustable states.

## Canonical Right-Panel Stage Order

The first implementation order is:

```text
0. Working Base
1. Tone
2. Face Shape
3. Face Detail
4. Skin
5. Blemish
6. Wrinkle
7. Makeup
8. Hair
9. Background
10. Text and non-raster display overlays
```

Reasons:

- geometry stages run before local skin and makeup effects.
- Background runs after subject retouch so a white or replaced background survives later face changes.
- Text remains a separate object layer and is composed for display and save.

This order is a contract. Changing it later requires cache invalidation and persisted-state migration.

## Persistent Photo Edit State

Each photo requires one document-level state owner.

Proposed shape:

```text
PhotoEditState
  PhotoPath
  StateVersion
  BaseRevision
  ToneState
  FaceShapeState
  FaceDetailState
  SkinState
  BlemishState
  WrinkleState
  MakeupState
  HairState
  BackgroundState
  TextItems
  FlattenBarriers
```

Each stage state must:

- define an explicit neutral value.
- support equality or a stable state hash.
- be immutable after publication.
- be serializable.
- contain all values needed to reproduce the stage.
- avoid references to temporary WPF controls.

## Required New State Models

### Tone State

Must include:

- curve points for All, Red, Green, and Blue channels
- curve strength
- exposure
- contrast
- saturation
- white balance
- sharpness

### Face Shape State

Must combine all modes into one snapshot:

- symmetry
- align or upper-face control
- cheek
- bone
- jaw
- chin
- face tilt
- face turn
- head tilt

Separate mode-specific session base images must not remain the source of truth.

### Face Detail State

The existing 41-value snapshot can become the persistent stage state.

The temporary Face Detail session base must be replaced by an upstream stage cache reference.

### Background State

Must include:

- active mode
- solid color
- picked color result
- background image path or stable asset id
- opacity
- edge control
- boundary cleanup
- edge blur
- alpha shrink
- softness
- alpha gamma

## Stage Cache Contract

Each selected photo may own transient cache entries:

```text
StageCacheEntry
  StageId
  Quality
  InputRevision
  StateRevision
  OutputRevision
  Bitmap
  AnalysisRevision
  IsValid
```

A cache entry is valid only when:

- its input revision matches the upstream output revision.
- its state revision matches the current stage state.
- required analysis data matches the input geometry revision.
- its render quality matches the request.

## Incremental Render Rules

### Adding Or Changing A Later Stage

Use the last valid upstream cache and calculate only the changed stage and downstream stages that already have active state.

Example:

```text
Skin is valid
-> user activates Blemish
-> reuse Skin cache
-> calculate Blemish only
```

### Changing An Earlier Stage

Invalidate that stage and all downstream stages.

Start from the nearest valid upstream cache.

Example:

```text
Background is white
-> user changes Mouth Corner in Face Detail
-> reuse Face Shape cache
-> recalculate Face Detail
-> recalculate active Skin through Hair stages
-> reapply stored white Background state
```

Tone and Face Shape are not recalculated in this example because their upstream caches remain valid.

### Reset

Set only the target stage to its neutral state.

Invalidate the target stage and its downstream range.

Do not remove history entries from unrelated stages.

### No Effective State

A neutral stage may pass through its upstream bitmap without allocating a new bitmap.

Its output revision must still identify the upstream revision used.

## Dirty-Range Invalidation

| Change | First Dirty Stage | Required Downstream Work |
|---|---|---|
| Working Base | Tone | all active stages |
| Tone | Tone | Face Shape through Background |
| Face Shape | Face Shape | Face Detail through Background |
| Face Detail | Face Detail | Skin through Background |
| Skin | Skin | Blemish through Background |
| Blemish | Blemish | Wrinkle through Background |
| Wrinkle | Wrinkle | Makeup through Background |
| Makeup | Makeup | Hair and Background |
| Hair | Hair | Background |
| Background | Background | Background only |

Inactive downstream stages are skipped.

## Analysis And Landmark Revisions

Landmark and mask data must be versioned separately from bitmap caches.

### Stable Across Tone

Tone-only changes do not require MediaPipe landmark re-analysis.

### Geometry Changes

Face Shape and geometry-producing Face Detail operations change the subject geometry.

The pipeline must do one of the following:

1. transform the landmark map with the geometry operation, or
2. re-run landmark analysis on the geometry-stage output.

The first implementation may use transformed landmarks where the current warp already provides deterministic point movement. Re-analysis is the fallback when transformed points are incomplete.

### Pixel Retouch Stages

Skin, Blemish, Wrinkle, Makeup, and color-only Hair operations may reuse the current geometry revision.

Hair geometry operations must publish a new geometry revision before Background masking.

### Background

Background matte and edge caches depend on the latest subject geometry revision.

They must be invalidated after geometry changes.

## Preview Rendering

1. Update stage state immediately.
2. Increment the stage state revision.
3. Cancel older preview work for the same photo.
4. Find the nearest valid upstream preview cache.
5. Recalculate the dirty range at preview resolution.
6. Publish output only when the photo id, state version, and render token still match.
7. Keep the last good preview visible until a newer valid preview is ready.

Preview must never write a partial downstream result into committed full-resolution state.

## Commit Rendering

Commit begins on mouse release, keyboard commit, button command, or explicit Apply.

Commit rules:

- use the same state snapshot and stage order as preview.
- start from the nearest valid full-resolution upstream cache when available.
- otherwise rebuild from the nearest approved checkpoint.
- publish one atomic final image.
- capture one editor-history state after successful publication.
- never clear another stage's state during history capture.

## Cache Memory Policy

Caching every full-resolution stage is unsafe for large portraits.

### Preview Cache

- Keep active-stage preview caches for the currently selected photo.
- Use an LRU policy when the configured memory budget is exceeded.
- Clear preview bitmaps for deselected photos while retaining persistent edit state.

### Full-Resolution Cache

- Keep the final committed bitmap.
- Keep a small LRU set of useful upstream checkpoints.
- Prefer geometry-boundary checkpoints because they save expensive downstream work.
- Do not retain every full-resolution stage unconditionally.
- Make the memory budget measurable and configurable.

### Disk Persistence

- Persist state snapshots and the final history image.
- Rebuild transient caches after restart.
- Do not persist all stage caches.

## History Model

`EditorHistoryState` must store a complete `PhotoEditStateSnapshot`.

Undo and redo must:

1. restore the complete edit state.
2. compare the restored state with the current state.
3. find the earliest changed stage.
4. rerender only the resulting dirty range.

History capture must be side-effect free.

The current behavior that clears live section states inside history capture must be removed after the unified state owner is active.

## Photo Switching And Restart

On photo switch:

- persist the current `PhotoEditState`.
- cancel outstanding renders for the previous photo.
- release transient preview caches according to the memory policy.
- restore the selected photo's complete state.
- show the stored final committed image immediately.
- lazily rebuild caches as needed.

On application restart:

- restore the complete state snapshot.
- restore the last committed image for immediate display.
- do not reset Tone, Face Shape, Face Detail, or Background controls.

## Save Contract

Save must:

- wait for or request a full-resolution render of the latest state version.
- reject an obsolete preview token.
- compose text objects at the final save boundary.
- produce the same visible result as the latest committed preview.
- leave editable state unchanged after save.

## Toolbox And Crop Strategy

### Non-Destructive Tools

Selection, navigation, sampling, paths, and measurement must not invalidate retouch states.

### Destructive Pixel Tools

Brush, Eraser, Stamp, Healing, Blur, Sharpen, Dodge, Burn, Liquify, Fill, and Gradient currently produce flattened raster results.

Migration has two phases.

#### Phase A: Explicit Barrier

- represent the operation as a named flatten barrier.
- preserve all stage values in history.
- block unsafe upstream adjustment when replay cannot preserve the pixel operation.
- show a clear status instead of silently clearing values.

#### Phase B: Replayable Operation Nodes

- store tool parameters, masks, coordinates, and source samples where practical.
- replay the operation as an ordered graph node.
- invalidate from the earliest affected node.

### Crop

Crop changes dimensions and coordinate systems.

It is an explicit base-revision operation.

The initial safe policy is:

- perform Crop before adjustable geometry and local-mask stages, or
- require an explicit flatten checkpoint before Crop.

Crop must never silently discard retouch state.

## Error Handling

- retain the last good committed image after a render failure.
- retain all stage states after a failure.
- identify the failed stage in status text.
- never publish an out-of-date render.
- never substitute a different render order after a failure.

## Migration Plan

### Phase 0: Baseline

- add cross-tab reproduction scenarios.
- record preview and commit timings.
- add render-stage diagnostic logging without screenshots.

### Phase 1: State Owner And Pipeline Shell

- add `PhotoEditState`.
- add stage ids, revisions, cache entries, and dirty-range calculation.
- keep existing renderers unchanged behind stage adapters.

### Phase 2: Existing Connected Sections

- move Skin, Blemish, Wrinkle, Makeup, and Hair into the pipeline service.
- preserve their current fixed order and visual formulas.
- remove history-capture side effects for these stages.

### Phase 3: Tone

- add persistent Tone state.
- remove the stale photo-selection base.
- route Tone preview and commit through the pipeline.

### Phase 4: Face Shape

- replace mode-specific session bases with one combined Face Shape state.
- publish a geometry revision and transformed landmark map.

### Phase 5: Face Detail

- persist the existing 41-value snapshot.
- replace its temporary base with the Face Shape output cache.

### Phase 6: Background

- persist mode, resource, and seven adjustment values.
- make Background the final raster stage.

### Phase 7: History, Photo Switching, Restart, And Save

- persist complete edit-state snapshots.
- restore controls and stage state for every tab.
- render from the earliest dirty stage after undo or redo.
- enforce latest-state full-resolution save.

### Phase 8: Toolbox Boundaries

- implement explicit flatten barriers.
- stop silent state clearing.
- migrate high-value pixel tools to replayable nodes incrementally.

### Phase 9: Cleanup

- remove obsolete session-base fields and old recomposition helpers.
- remove compatibility code only after all acceptance checks pass.
- synchronize contract documents with the source.

## Acceptance Gate

Implementation is complete only when:

- every right-panel value survives cross-tab editing.
- `A -> B -> A` keeps both A and B for all active stage pairs.
- reset removes only the selected stage.
- undo and redo restore values and pixels together.
- photo switching and restart restore values and pixels together.
- save matches the latest committed state.
- stale renders never replace newer results.
- preview performance stays within an approved measured budget.
- no existing visual formula is tuned during the state-pipeline migration unless required for correctness.

