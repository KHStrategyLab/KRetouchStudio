# Eight Point Correction Lab

## Purpose

This lab measures whether an eight-point correction profile moves auto-detected points closer to the user's answer points.

The app must not know that the test image is the same picture. This keeps exact restore out of the test and measures only correction direction.

## Test Model

For each landmark:

```text
RawPoint = program ai_suggested point
AnswerPoint = user final point from answer JSON
PredictedPoint(weight) = RawPoint + CorrectionVector * weight
RawError = distance(RawPoint, AnswerPoint)
PredictedError(weight) = distance(PredictedPoint(weight), AnswerPoint)
```

The correction is useful when:

```text
PredictedError(weight) < RawError
```

## Input Folders

Answer folder:

```text
C:\Users\STUDIO304\source\repos\DATA_DANGER\2026-06-16-1
```

Test folder:

```text
C:\Users\STUDIO304\source\repos\DATA_DANGER\2026-06-16-3
```

The answer folder contains the user's known good final points.

The test folder contains renamed or copied images that the app treats as unknown images.

## Match Strategy

Initial matching should be manual or rule-based by file order.

Recommended first version:

```text
Sort answer JSON files by name
Sort test JSON files by name
Pair by index
```

This is enough for controlled lab folders where the same image set was processed in the same order.

Future version:

```text
Use image fingerprint or pHash
Use crop dimensions and visual hash
Match even if file names and folders changed
```

## Weight Candidates

Use these candidate weights first:

```text
0.00
0.20
0.40
0.65
0.80
1.00
```

## Current Candidate Policy

```text
left_eye:        0.65
right_eye:       0.65
nose_tip:        0.80
chin:            0.40
mouth_left:      0.00
mouth_right:     0.00
mouth_center:    0.00
clothing_start:  0.00
```

## Expected Report

The lab should generate a CSV or Markdown table:

```text
point, raw_error, w0.20_error, w0.40_error, w0.65_error, w0.80_error, w1.00_error, best_weight
left_eye, ...
right_eye, ...
nose_tip, ...
mouth_left, ...
mouth_right, ...
mouth_center, ...
chin, ...
clothing_start, ...
```

Also generate a summary:

```text
point, sample_count, best_weight_median, raw_error_median, best_error_median, improvement_percent
```

## Interpretation

Keep correction if:

```text
best_error_median is lower than raw_error_median
```

Disable correction if:

```text
best_weight is usually 0.00
```

Use a lower weight if:

```text
high weight improves some samples but makes many samples worse
```

## Current Observations

Original data compared with cropped data showed broad improvement.

The weighted correction test showed:

```text
Eyes: stable
Nose: strong improvement
Chin: slight improvement
Mouth: worse when globally weighted
Clothing start: not reliable because it is generated from face/chin geometry, not detected from clothing pixels
```

## Next Implementation

Create a standalone lab runner:

```text
Tools\EightPointCorrectionLabRunner.cs
```

The runner should:

```text
Read answer folder JSON files
Read test folder JSON files
Pair files by sorted order
Build correction profile from answer or selected profile folder
Compute errors for weight candidates
Write report to docs\lab_reports\
```

The runner should not change app behavior.

