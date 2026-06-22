# MediaPipe Model Drop Folder

Place Google MediaPipe model files here.

Starter files:

- `face_detector.tflite`
- `image_segmenter.tflite`
- `face_landmarker.task`

Current source models:

- Face Detector: MediaPipe BlazeFace short-range float16.
- Image Segmenter: MediaPipe SelfieSegmenter square float16.
- Face Landmarker: MediaPipe FaceLandmarker float16 task bundle.

The app keeps MediaPipe as an external helper path first:

- Face Detector: lightweight face box and six key points.
- Image Segmenter: person/background alpha mask.
- Face Landmarker: yaw, pitch, roll, and 3D face landmarks.

Model files are kept here for reproducible prototype setup. Before commercial packaging or redistribution, verify the current model license and distribution policy from the official model source.
