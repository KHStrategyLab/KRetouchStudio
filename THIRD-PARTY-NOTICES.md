# Third-Party Notices

## Font Awesome Free

KRetouchStudio uses SVG path data from Font Awesome Free for the `link` and `link-slash` icons.

- Copyright: Fonticons, Inc.
- Source: https://fontawesome.com
- License: https://fontawesome.com/license/free
- Icon license: Creative Commons Attribution 4.0 International (CC BY 4.0)
- Font license: SIL Open Font License 1.1
- Code license: MIT License

## Microsoft .NET Runtime

KRetouchStudio release packages may include the self-contained Microsoft .NET runtime.

- Source: https://github.com/dotnet/runtime
- License: MIT

## OpenCvSharp

KRetouchStudio uses OpenCvSharp and its Windows native runtime for healing and image-processing operations.

- Source: https://github.com/shimat/opencvsharp
- License: Apache License 2.0

## OpenCV

The OpenCvSharp Windows runtime redistributes OpenCV native binaries.

- Source: https://github.com/opencv/opencv
- License: Apache License 2.0

## MediaPipe

KRetouchStudio uses the MediaPipe Python package and MediaPipe Tasks model files for face detection, segmentation, and face landmarks.

- Source: https://github.com/google-ai-edge/mediapipe
- License: Apache License 2.0
- Model redistribution terms must be verified against each original model card before commercial release.

## BiRefNet Lite Matting

KRetouchStudio downloads and uses ZhengPeng7/BiRefNet_lite-matting for person alpha matting.

- Model: https://huggingface.co/ZhengPeng7/BiRefNet_lite-matting
- License declared by the model repository: MIT

## Python Runtime Dependencies

The Python environment is installed separately and is not embedded in the release ZIP. Runtime packages and tested versions are listed in requirements.txt. Their license files and transitive dependency notices must be collected from the exact environment used for commercial deployment.

## Release Compliance Status

This inventory is a technical dependency record, not legal approval. Before commercial redistribution:

- archive the exact model source URLs and model cards;
- include the full applicable license texts in the distributed package;
- generate a license report for the resolved Python dependency environment;
- obtain project-owner approval for the final notices bundle.

