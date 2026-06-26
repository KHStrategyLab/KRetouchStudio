# Daily Retouch Stability Standard

This document defines the stability baseline for tools used in every studio session:

- Background replacement
- Face tilt / face balance / upper balance
- Skin cleanup
- Mask-assisted retouching and future AI cleanup tools

The target is not only "the feature works." The target is "the feature can be used repeatedly every day without freezing the app, flooding GPU/Python work, or losing the current photo state."

## Operating Standard

Every high-use retouch feature must follow these rules.

1. Drag preview must use a lightweight source only.
   - Use `PreviewProxy1200` or another explicit proxy.
   - Do not run full-resolution image processing during pointer drag.
   - Do not start AI/Python/GPU generation from a drag event unless the work is already single-flight and explicitly background-only.

2. Commit/apply must be serialized per feature group.
   - One background commit at a time.
   - One face-shape commit at a time.
   - One skin cleanup commit at a time.
   - New commit requests may queue or replace the pending request, but they must not run in parallel.

3. Preview results must be versioned.
   - If the selected photo, tool mode, source image, or render version changes, the old result must not be assigned to the UI.
   - A late result may be discarded silently.

4. External workers must have a timeout/cancel path.
   - Python, GPU inference, model warmup, and helper processes must not wait forever.
   - Worker shutdown must be available on app close and on repeated failure.

5. AI/mask generation must be single-flight.
   - For the same photo and same engine, only one generation may run.
   - If an artifact already exists and is valid, reuse it.
   - If generation failed for the same photo, do not retry infinitely from slider or preview events.

6. UI thread must only coordinate.
   - UI thread may read current values, update labels, swap image sources, and write history state.
   - Pixel loops, full-resolution compositing, mask refinement, warp rendering, and heavy bitmap conversion must run off the UI thread.

7. Cleanup must be deterministic.
   - On app close, tool switch, photo switch, or pipeline reset, queued preview work must be invalidated.
   - Long-running workers must not keep stale photo state alive.

## Job Classes

Use these classes when reviewing or implementing a feature.

| Class | Examples | Required behavior |
| --- | --- | --- |
| `DragPreview` | slider drag, brush hover, live preview | proxy only, latest-result-wins, no history write |
| `CommitRender` | mouse-up apply, button apply | serialized, full-res allowed, history write allowed |
| `ArtifactBuild` | alpha matte, subject mask, skin mask, face landmarks | single-flight, cached, failure-cached |
| `ExternalWorker` | BiRefNet, Python helper, GPU inference | timeout, cancel/shutdown, one worker gate |
| `Warmup` | model load, preview cache build | low priority, never blocks UI |

## Daily-Use Feature Priorities

### 1. Background Replacement

Required:

- Background alpha generation is single-flight.
- Drag sliders use cached alpha and 1200 preview only.
- Full-resolution composition is not performed during drag.
- Image background decode/composite is bounded to the current render version.
- BiRefNet worker has timeout/cancel/shutdown behavior.

Known risk to audit next:

- Full-resolution background composition should run off the UI thread.
- BiRefNet requests currently need an explicit timeout policy.

### 2. Face Tilt / Face Balance / Upper Balance

Required:

- Drag preview uses 1200 transient preview only.
- Mouse-up commit runs one full-resolution render at a time.
- Face, head, hair, shoulder, and upper-body balance must not cross-commit through the wrong active mode.
- If landmarks/masks are stale after another tool changed the photo, rebuild or reject the preview result.

Known risk to audit next:

- Commit render paths should be reviewed for common serialization and cancellation.
- Head/hair/upper balance mask reuse must be checked after background or crop changes.

### 3. Skin Cleanup

Required before production use:

- Brush movement must not start full-resolution cleanup.
- Skin mask generation must be cached and single-flight.
- Cleanup preview must be local/proxy-based.
- Commit must be serialized and version-checked.
- AI cleanup, if added, must use timeout and failure cache.

## Review Checklist

Before a feature is considered daily-use safe, verify:

- [ ] Drag path does not call full-resolution render.
- [ ] Drag path does not launch uncached AI/Python work repeatedly.
- [ ] Commit path is serialized.
- [ ] External worker has timeout/cancel.
- [ ] Result assignment checks selected photo and render version.
- [ ] History writes happen only on commit/apply.
- [ ] Cached artifacts are keyed by photo, engine, and size where needed.
- [ ] Failed artifacts are not retried endlessly from preview events.
- [ ] App close or photo switch invalidates stale work.
- [ ] Build passes on x64.

## Unsafe Patterns

Avoid these patterns in production retouch tools:

- Calling `GetOrCreate...Async()` from every slider value change when the "create" path is heavy.
- Running full-resolution pixel loops directly inside UI event handlers.
- Starting a new Python process or GPU inference per preview tick.
- Applying a late preview result after the selected photo changed.
- Writing history during drag preview.
- Retrying a failed AI artifact generation every time the UI refreshes.

## Patch Order Standard

For stability patches, use small orders in this sequence:

1. Identify the workflow and first unsafe state.
2. Add or confirm single-flight gate.
3. Add version check / stale result rejection.
4. Move heavy preview work to proxy or background task.
5. Add timeout/cancel for external work.
6. Build x64.
7. Let the project director perform visual confirmation.

Do not combine behavior redesign with stability cleanup unless explicitly approved.
