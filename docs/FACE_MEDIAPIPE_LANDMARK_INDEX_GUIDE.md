# Face MediaPipe Landmark Index Guide

This document is the code-facing landmark index guide for the current KRetouchStudio Face Shape work.

Source authority:

- Runtime arrays: `UI/FaceShape/MainWindow.FaceShape.cs`
- Retouch planning note: `BEAUTY_RETOUCHING_TECHNIQUES.md`
- Related design rule: `FACE_UPDN_3D_PROJECTION_RULE.md`

## 1. Scope Rule

MediaPipe face landmarks must be treated as detector data, not as final visible contour truth.

- `0-467`: canonical MediaPipe face mesh points.
- `468-477`: optional iris / refined eye points.
- Current Face Shape runtime mostly uses `0-467`.
- Extra iris points are reference data only unless a dedicated eye tool explicitly owns them.
- Do not feed all `478` points blindly into a camera solver, final mask, or heavy per-frame warp.
- For jawline, hairline, lips, and soft skin boundaries, use local image evidence after landmark routing.

## 2. Core Anchors

These points are useful as stable orientation and pivot references.

| Role | Indices | Current runtime use |
| --- | --- | --- |
| Left outer eye corner | `33` | Head tilt / pose anchor |
| Right outer eye corner | `263` | Head tilt / pose anchor |
| Brow center pivot | `8` | Head pose pivot |
| Nose tip | `4` | Head tilt / pose anchor |
| Chin tip | `152` | Head tilt / lower-face anchor |
| Left mouth corner | `61` | Head tilt / pose anchor |
| Right mouth corner | `291` | Head tilt / pose anchor |
| Left iris center | `468` | Optional eye reference only |
| Right iris center | `473` | Optional eye reference only |

Do not use iris points as rigid face-mask anchors for the current Face Shape renderer.

## 3. Face Oval

Runtime owner: `FaceShapeHeadTiltFaceOvalIndices`.

```text
10, 338, 297, 332, 284, 251, 389, 356, 454, 323, 361, 288,
397, 365, 379, 378, 400, 377, 152, 148, 176, 149, 150, 136,
172, 58, 132, 93, 234, 127, 162, 21, 54, 103, 67, 109
```

Use this as a face-outline routing path, not as the final visible cutout edge.

## 4. Symmetry

Runtime owner: `FaceShapeSymmetryMidlineIndices`.

```text
10, 168, 6, 197, 195, 5, 4, 1, 19, 94, 2, 152
```

Runtime owner: `FaceShapeSymmetryPairs`.

```text
(33,263), (133,362), (159,386), (145,374), (61,291), (78,308),
(70,300), (63,293), (105,334), (66,296), (107,336),
(234,454), (93,323), (132,361), (58,288), (172,397),
(136,365), (150,379), (149,378), (176,400), (148,377)
```

## 5. V-Line / Jaw

Runtime owner: `FaceShapeJawPairs`.

```text
(234,454,0.20), (93,323,0.42), (132,361,0.58), (58,288,0.62),
(172,397,0.82), (136,365,1.00), (150,379,1.00),
(149,378,0.88), (176,400,0.62), (148,377,0.32)
```

Runtime owner: `FaceShapeJawAnchorIndices`.

```text
1, 4, 5, 6, 13, 14, 17, 78, 308, 152, 199, 200
```

## 6. Chin

Runtime owner: `FaceShapeChinPairs`.

```text
(136,365,0.20), (150,379,0.36), (149,378,0.62),
(176,400,0.88), (148,377,1.00)
```

Runtime owner: `FaceShapeChinCenterIndices`.

```text
152, 175, 199, 200
```

Runtime owner: `FaceShapeChinAnchorIndices`.

```text
1, 4, 5, 6, 13, 14, 17, 61, 78, 93, 132, 172, 234,
288, 291, 308, 323, 361, 397, 454
```

## 7. Cheek

Runtime owner: `FaceShapeCheekPairs`.

```text
(234,454,0.35), (93,323,0.55), (132,361,0.45), (50,280,0.35),
(101,330,0.52), (118,347,0.78), (123,352,1.00),
(187,411,0.82), (205,425,1.00), (206,426,0.86),
(207,427,0.64), (213,433,0.44)
```

Runtime owner: `FaceShapeCheekAnchorIndices`.

```text
1, 4, 5, 6, 13, 14, 17, 33, 61, 78, 133, 152,
159, 168, 263, 291, 308, 362, 386
```

## 8. Bone / Cheekbone

Runtime owner: `FaceShapeBonePairs`.

```text
(127,356,0.50), (234,454,0.86), (93,323,1.00), (132,361,0.56),
(50,280,0.40), (101,330,0.55), (118,347,0.84),
(123,352,0.92), (187,411,0.58), (205,425,0.45)
```

Runtime owner: `FaceShapeBoneAnchorIndices`.

```text
1, 4, 5, 6, 10, 13, 14, 17, 33, 61, 78, 133,
152, 159, 168, 199, 263, 291, 308, 362, 386
```

## 9. Face Turn / Tilt Support

Runtime owner: `FaceShapeFaceTurnCentralIndices`.

```text
1, 2, 4, 5, 6, 19, 94, 168, 195, 197
```

Runtime owner: `FaceShapeFaceTurnFeatureEdgeIndices`.

```text
33, 46, 61, 70, 78, 107, 133, 152, 172, 234,
263, 276, 291, 300, 308, 336, 362, 397, 454
```

Runtime owner: `FaceShapeFaceTurnFeatherBoostIndices`.

```text
58, 93, 132, 136, 148, 149, 150, 172, 176, 234,
288, 323, 361, 365, 377, 378, 379, 397, 400, 454
```

Runtime owner: `FaceShapeFaceTiltPivotIndices`.

```text
1, 4, 5, 6, 168, 195, 197
```

Runtime owner: `FaceShapeFaceTiltAnchorIndices`.

```text
10, 58, 67, 93, 109, 132, 136, 148, 149, 150, 152,
162, 172, 176, 199, 200, 234, 288, 297, 323, 338,
361, 365, 377, 378, 379, 389, 397, 400, 454
```

## 10. Retouch Planning References

These are planning references from the retouch note. They are not automatically runtime Face Shape ownership unless a tool explicitly adopts them.

| Retouch use | Reference indices | Rule |
| --- | --- | --- |
| Hair volume top pin | `10` | Pin the forehead line before lifting outer hair pixels. |
| Side hair volume pin | `234`, `454` | Keep cheekbone area stable while expanding upper temple hair. |
| Upper temple / side hair guide | `109`, `338` | Use as outside-hair routing references, not face-mask anchors. |
| Hairline correction | `10`, `109`, `338` | Pull hairline only after face/forehead safety is confirmed. |

## 11. Working Rule

When designing new Face Shape or beauty-retouch tools:

1. Start from the smallest code-owned landmark group above.
2. Use landmarks for routing, anchors, and approximate ROI only.
3. Resolve the final visible boundary from local image evidence.
4. Keep `468-477` iris points out of rigid face-shape geometry.
5. Keep preview routing separate from commit/save/history behavior unless that is the explicit task.
