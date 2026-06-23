#!/usr/bin/env python
"""Run KRetouch Studio MediaPipe checks against one image.

This helper is intentionally narrow: it only verifies model connectivity and
writes detection artifacts for the C# app to inspect later.
"""

from __future__ import annotations

import argparse
import json
import math
import os
import sys
import traceback
from pathlib import Path
from typing import Any

import numpy as np
from PIL import Image

import mediapipe as mp
from mediapipe.tasks import python
from mediapipe.tasks.python import vision


FACE_DETECTOR_NAME = "face_detector.tflite"
IMAGE_SEGMENTER_NAME = "image_segmenter.tflite"
FACE_LANDMARKER_NAME = "face_landmarker.task"

FACE_OVERLAY_GROUPS: list[tuple[str, list[int], bool]] = [
    ("left_eye", [33, 246, 161, 160, 159, 158, 157, 173, 133, 155, 154, 153, 145, 144, 163, 7], True),
    ("right_eye", [362, 398, 384, 385, 386, 387, 388, 466, 263, 249, 390, 373, 374, 380, 381, 382], True),
    ("left_eyebrow", [70, 63, 105, 66, 107, 55, 65, 52, 53, 46], False),
    ("right_eyebrow", [336, 296, 334, 293, 300, 285, 295, 282, 283, 276], False),
    ("nose_bridge", [168, 6, 197, 195, 5, 4, 1, 19, 94, 2], False),
    ("nose_base", [98, 97, 2, 326, 327], False),
    ("mouth_outer", [61, 146, 91, 181, 84, 17, 314, 405, 321, 375, 291, 409, 270, 269, 267, 0, 37, 39, 40, 185], True),
    ("mouth_inner", [78, 95, 88, 178, 87, 14, 317, 402, 318, 324, 308, 415, 310, 311, 312, 13, 82, 81, 80, 191], True),
    ("chin_jaw", [234, 93, 132, 58, 172, 136, 150, 149, 176, 148, 152, 377, 400, 378, 379, 365, 397, 288, 361, 323, 454], False),
]


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="KRetouch Studio MediaPipe helper")
    parser.add_argument("--image", required=True, help="Input image path")
    parser.add_argument("--models", required=True, help="MediaPipe model folder")
    parser.add_argument("--output", required=True, help="Output artifact folder")
    return parser.parse_args()


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")


def model_path(models_dir: Path, file_name: str) -> Path:
    return models_dir / file_name


def detection_score(detection: Any) -> float | None:
    categories = getattr(detection, "categories", None)
    if not categories:
        return None

    first = categories[0]
    return float(getattr(first, "score", 0.0))


def run_face_detector(mp_image: mp.Image, models_dir: Path, output_dir: Path) -> dict[str, Any]:
    detector_path = model_path(models_dir, FACE_DETECTOR_NAME)
    options = vision.FaceDetectorOptions(
        base_options=python.BaseOptions(model_asset_path=str(detector_path)),
        running_mode=vision.RunningMode.IMAGE,
        min_detection_confidence=0.35,
    )

    with vision.FaceDetector.create_from_options(options) as detector:
        result = detector.detect(mp_image)

    faces: list[dict[str, Any]] = []
    for detection in result.detections:
        box = detection.bounding_box
        keypoints = []
        for point in detection.keypoints or []:
            keypoints.append(
                {
                    "x": float(point.x),
                    "y": float(point.y),
                    "label": getattr(point, "label", None),
                }
            )

        faces.append(
            {
                "score": detection_score(detection),
                "box": {
                    "x": int(box.origin_x),
                    "y": int(box.origin_y),
                    "width": int(box.width),
                    "height": int(box.height),
                },
                "keypoints": keypoints,
            }
        )

    payload = {
        "status": "ok",
        "model": str(detector_path),
        "face_count": len(faces),
        "faces": faces,
    }
    write_json(output_dir / "face_box.json", payload)
    return payload


def run_image_segmenter(mp_image: mp.Image, models_dir: Path, output_dir: Path) -> dict[str, Any]:
    segmenter_path = model_path(models_dir, IMAGE_SEGMENTER_NAME)
    options = vision.ImageSegmenterOptions(
        base_options=python.BaseOptions(model_asset_path=str(segmenter_path)),
        running_mode=vision.RunningMode.IMAGE,
        output_confidence_masks=True,
        output_category_mask=False,
    )

    with vision.ImageSegmenter.create_from_options(options) as segmenter:
        result = segmenter.segment(mp_image)

    confidence_masks = list(result.confidence_masks or [])
    if not confidence_masks:
        raise RuntimeError("Image segmenter returned no confidence masks.")

    person_mask_index = 1 if len(confidence_masks) > 1 else 0
    person_mask = np.array(confidence_masks[person_mask_index].numpy_view())
    if person_mask.ndim == 3 and person_mask.shape[2] == 1:
        person_mask = person_mask[:, :, 0]

    alpha = np.clip(person_mask * 255.0, 0.0, 255.0).astype(np.uint8)
    alpha_path = output_dir / "person_alpha.png"
    Image.fromarray(alpha, mode="L").save(alpha_path)

    payload = {
        "status": "ok",
        "model": str(segmenter_path),
        "mask_count": len(confidence_masks),
        "person_mask_index": person_mask_index,
        "alpha_path": str(alpha_path),
        "width": int(alpha.shape[1]),
        "height": int(alpha.shape[0]),
    }
    write_json(output_dir / "person_alpha.json", payload)
    return payload


def matrix_to_list(matrix: Any) -> list[list[float]]:
    if hasattr(matrix, "data"):
        raw = np.array(matrix.data, dtype=float)
    else:
        raw = np.array(matrix, dtype=float)

    raw = raw.reshape((4, 4))
    return raw.tolist()


def rotation_matrix_to_euler_degrees(matrix: list[list[float]]) -> dict[str, float]:
    rotation = np.array(matrix, dtype=float)[:3, :3]
    sy = math.sqrt(rotation[0, 0] * rotation[0, 0] + rotation[1, 0] * rotation[1, 0])
    singular = sy < 1e-6

    if not singular:
        x = math.atan2(rotation[2, 1], rotation[2, 2])
        y = math.atan2(-rotation[2, 0], sy)
        z = math.atan2(rotation[1, 0], rotation[0, 0])
    else:
        x = math.atan2(-rotation[1, 2], rotation[1, 1])
        y = math.atan2(-rotation[2, 0], sy)
        z = 0.0

    return {
        "pitch": math.degrees(x),
        "yaw": math.degrees(y),
        "roll": math.degrees(z),
    }


def landmark_to_dict(point: Any) -> dict[str, float]:
    return {
        "x": float(point.x),
        "y": float(point.y),
        "z": float(point.z),
    }


def overlay_group_to_dict(name: str, indices: list[int], closed: bool, landmarks: Any) -> dict[str, Any]:
    points = [
        {
            "index": index,
            **landmark_to_dict(landmarks[index]),
        }
        for index in indices
        if index < len(landmarks)
    ]
    return {
        "name": name,
        "closed": closed,
        "points": points,
    }


def load_mp_image(image_path: Path) -> mp.Image:
    with Image.open(image_path) as image:
        rgb_image = image.convert("RGB")
        data = np.array(rgb_image)

    return mp.Image(image_format=mp.ImageFormat.SRGB, data=data)


def run_face_landmarker(mp_image: mp.Image, models_dir: Path, output_dir: Path) -> dict[str, Any]:
    landmarker_path = model_path(models_dir, FACE_LANDMARKER_NAME)
    options = vision.FaceLandmarkerOptions(
        base_options=python.BaseOptions(model_asset_path=str(landmarker_path)),
        running_mode=vision.RunningMode.IMAGE,
        num_faces=1,
        min_face_detection_confidence=0.35,
        min_face_presence_confidence=0.35,
        min_tracking_confidence=0.35,
        output_face_blendshapes=False,
        output_facial_transformation_matrixes=True,
    )

    with vision.FaceLandmarker.create_from_options(options) as landmarker:
        result = landmarker.detect(mp_image)

    faces = []
    matrices = list(result.facial_transformation_matrixes or [])
    for index, landmarks in enumerate(result.face_landmarks or []):
        matrix = matrix_to_list(matrices[index]) if index < len(matrices) else None
        pose = rotation_matrix_to_euler_degrees(matrix) if matrix is not None else None
        faces.append(
            {
                "index": index,
                "landmark_count": len(landmarks),
                "pose_degrees": pose,
                "matrix": matrix,
                "sample_landmarks": {
                    "nose_tip_1": landmark_to_dict(landmarks[1]) if len(landmarks) > 1 else None,
                    "chin_152": landmark_to_dict(landmarks[152]) if len(landmarks) > 152 else None,
                    "forehead_10": landmark_to_dict(landmarks[10]) if len(landmarks) > 10 else None,
                },
                "overlay_landmarks": {
                    "groups": [
                        overlay_group_to_dict(name, indices, closed, landmarks)
                        for name, indices, closed in FACE_OVERLAY_GROUPS
                    ],
                },
            }
        )

    payload = {
        "status": "ok",
        "model": str(landmarker_path),
        "face_count": len(faces),
        "pose_source": "facial_transformation_matrix_approx",
        "faces": faces,
    }
    write_json(output_dir / "face_pose.json", payload)
    return payload


def run_step(name: str, func: Any, mp_image: mp.Image, models_dir: Path, output_dir: Path) -> dict[str, Any]:
    try:
        result = func(mp_image, models_dir, output_dir)
        return {"name": name, "status": "ok", "result": result}
    except Exception as exc:  # noqa: BLE001 - errors are reported to C# as diagnostics.
        return {
            "name": name,
            "status": "error",
            "error": str(exc),
            "traceback": traceback.format_exc(),
        }


def main() -> int:
    args = parse_args()
    image_path = Path(args.image)
    models_dir = Path(args.models)
    output_dir = Path(args.output)
    output_dir.mkdir(parents=True, exist_ok=True)

    summary: dict[str, Any] = {
        "status": "pending",
        "image": str(image_path),
        "models": str(models_dir),
        "output": str(output_dir),
        "mediapipe_version": mp.__version__,
        "steps": [],
    }

    if not image_path.exists():
        summary["status"] = "error"
        summary["error"] = f"Image not found: {image_path}"
        write_json(output_dir / "mediapipe_result.json", summary)
        return 2

    missing_models = [
        name
        for name in (FACE_DETECTOR_NAME, IMAGE_SEGMENTER_NAME, FACE_LANDMARKER_NAME)
        if not model_path(models_dir, name).exists()
    ]
    if missing_models:
        summary["status"] = "error"
        summary["error"] = "Missing model files."
        summary["missing_models"] = missing_models
        write_json(output_dir / "mediapipe_result.json", summary)
        return 3

    try:
        mp_image = load_mp_image(image_path)
    except Exception as exc:  # noqa: BLE001 - create a readable connection artifact.
        summary["status"] = "error"
        summary["error"] = str(exc)
        summary["traceback"] = traceback.format_exc()
        write_json(output_dir / "mediapipe_result.json", summary)
        return 4

    steps = [
        ("face_detector", run_face_detector),
        ("image_segmenter", run_image_segmenter),
        ("face_landmarker", run_face_landmarker),
    ]
    summary["steps"] = [run_step(name, func, mp_image, models_dir, output_dir) for name, func in steps]

    failed_steps = [step["name"] for step in summary["steps"] if step["status"] != "ok"]
    summary["status"] = "error" if failed_steps else "ok"
    summary["failed_steps"] = failed_steps
    write_json(output_dir / "mediapipe_result.json", summary)
    return 1 if failed_steps else 0


if __name__ == "__main__":
    os.environ.setdefault("TF_CPP_MIN_LOG_LEVEL", "2")
    sys.exit(main())
