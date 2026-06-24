#!/usr/bin/env python
"""Persistent MediaPipe worker for KRetouch Studio."""

from __future__ import annotations

import gc
import json
import os
import sys
import time
import traceback
from pathlib import Path
from typing import Any

os.environ.setdefault("TF_CPP_MIN_LOG_LEVEL", "2")

import mediapipe as mp
import numpy as np
from PIL import Image
from mediapipe.tasks import python
from mediapipe.tasks.python import vision

from mediapipe_helper import (
    FACE_DETECTOR_NAME,
    FACE_LANDMARKER_NAME,
    IMAGE_SEGMENTER_NAME,
    FACE_OVERLAY_GROUPS,
    all_landmarks_to_list,
    detection_score,
    landmark_to_dict,
    load_mp_image,
    matrix_to_list,
    model_path,
    overlay_group_to_dict,
    rotation_matrix_to_euler_degrees,
    write_json,
)


class MediaPipeWorker:
    def __init__(self) -> None:
        self.models_dir: Path | None = None
        self.face_detector: Any = None
        self.image_segmenter: Any = None
        self.face_landmarker: Any = None

    def ensure_loaded(self, models_dir: Path) -> float:
        if (
            self.models_dir == models_dir
            and self.face_detector is not None
            and self.image_segmenter is not None
            and self.face_landmarker is not None
        ):
            return 0.0

        self.unload()
        missing_models = [
            name
            for name in (FACE_DETECTOR_NAME, IMAGE_SEGMENTER_NAME, FACE_LANDMARKER_NAME)
            if not model_path(models_dir, name).exists()
        ]
        if missing_models:
            raise RuntimeError("Missing model files: " + ", ".join(missing_models))

        started = time.perf_counter()
        detector_path = model_path(models_dir, FACE_DETECTOR_NAME)
        segmenter_path = model_path(models_dir, IMAGE_SEGMENTER_NAME)
        landmarker_path = model_path(models_dir, FACE_LANDMARKER_NAME)

        detector_options = vision.FaceDetectorOptions(
            base_options=python.BaseOptions(model_asset_path=str(detector_path)),
            running_mode=vision.RunningMode.IMAGE,
            min_detection_confidence=0.35,
        )
        segmenter_options = vision.ImageSegmenterOptions(
            base_options=python.BaseOptions(model_asset_path=str(segmenter_path)),
            running_mode=vision.RunningMode.IMAGE,
            output_confidence_masks=True,
            output_category_mask=False,
        )
        landmarker_options = vision.FaceLandmarkerOptions(
            base_options=python.BaseOptions(model_asset_path=str(landmarker_path)),
            running_mode=vision.RunningMode.IMAGE,
            num_faces=1,
            min_face_detection_confidence=0.35,
            min_face_presence_confidence=0.35,
            min_tracking_confidence=0.35,
            output_face_blendshapes=False,
            output_facial_transformation_matrixes=True,
        )

        self.face_detector = vision.FaceDetector.create_from_options(detector_options)
        self.image_segmenter = vision.ImageSegmenter.create_from_options(segmenter_options)
        self.face_landmarker = vision.FaceLandmarker.create_from_options(landmarker_options)
        self.models_dir = models_dir
        return time.perf_counter() - started

    def unload(self) -> None:
        for task in (self.face_detector, self.image_segmenter, self.face_landmarker):
            try:
                if task is not None:
                    task.close()
            except Exception:
                pass

        self.models_dir = None
        self.face_detector = None
        self.image_segmenter = None
        self.face_landmarker = None
        gc.collect()

    def warmup(self, command: dict[str, Any]) -> dict[str, Any]:
        request_id = command.get("request_id")
        output_value = command.get("output")
        output_dir = Path(str(output_value)) if output_value else None
        models_dir = Path(str(command.get("models", "")))

        started = time.perf_counter()
        load_seconds = self.ensure_loaded(models_dir)
        payload: dict[str, Any] = {
            "request_id": request_id,
            "status": "ok",
            "engine": "MediaPipe",
            "run_mode": "worker",
            "command": "warmup",
            "worker_pid": os.getpid(),
            "models": str(models_dir),
            "mediapipe_version": mp.__version__,
            "load_seconds": round(load_seconds, 3),
            "total_seconds": round(time.perf_counter() - started, 3),
        }
        if output_dir is not None:
            output_dir.mkdir(parents=True, exist_ok=True)
            write_json(output_dir / "mediapipe_warmup_result.json", payload)

        return payload

    def run(self, command: dict[str, Any]) -> dict[str, Any]:
        request_id = command.get("request_id")
        image_path = Path(str(command.get("image", "")))
        models_dir = Path(str(command.get("models", "")))
        output_dir = Path(str(command.get("output", "")))
        output_dir.mkdir(parents=True, exist_ok=True)

        started = time.perf_counter()
        load_seconds = self.ensure_loaded(models_dir)
        if not image_path.exists():
            raise RuntimeError(f"Image not found: {image_path}")

        mp_image = load_mp_image(image_path)
        steps = [
            self._run_step("face_detector", self.run_face_detector, mp_image, output_dir),
            self._run_step("image_segmenter", self.run_image_segmenter, mp_image, output_dir),
            self._run_step("face_landmarker", self.run_face_landmarker, mp_image, output_dir),
        ]
        failed_steps = [step["name"] for step in steps if step["status"] != "ok"]

        summary: dict[str, Any] = {
            "request_id": request_id,
            "status": "error" if failed_steps else "ok",
            "engine": "MediaPipe",
            "run_mode": "worker",
            "worker_pid": os.getpid(),
            "image": str(image_path),
            "models": str(models_dir),
            "output": str(output_dir),
            "mediapipe_version": mp.__version__,
            "load_seconds": round(load_seconds, 3),
            "total_seconds": round(time.perf_counter() - started, 3),
            "steps": steps,
            "failed_steps": failed_steps,
        }
        write_json(output_dir / "mediapipe_result.json", summary)

        landmark_count = 0
        face_count = 0
        face_landmarker_step = next((step for step in steps if step["name"] == "face_landmarker"), None)
        if face_landmarker_step is not None and face_landmarker_step.get("status") == "ok":
            result = face_landmarker_step.get("result") or {}
            face_count = int(result.get("face_count") or 0)
            faces = result.get("faces") or []
            if faces:
                landmark_count = int(faces[0].get("landmark_count") or 0)

        return {
            "request_id": request_id,
            "status": summary["status"],
            "engine": "MediaPipe",
            "run_mode": "worker",
            "worker_pid": os.getpid(),
            "face_count": face_count,
            "landmark_count": landmark_count,
            "failed_steps": failed_steps,
            "load_seconds": summary["load_seconds"],
            "total_seconds": summary["total_seconds"],
        }

    def _run_step(self, name: str, func: Any, mp_image: mp.Image, output_dir: Path) -> dict[str, Any]:
        try:
            result = func(mp_image, output_dir)
            return {"name": name, "status": "ok", "result": result}
        except Exception as exc:  # noqa: BLE001 - errors are reported to C# as diagnostics.
            return {
                "name": name,
                "status": "error",
                "error": str(exc),
                "traceback": traceback.format_exc(),
            }

    def run_face_detector(self, mp_image: mp.Image, output_dir: Path) -> dict[str, Any]:
        if self.face_detector is None or self.models_dir is None:
            raise RuntimeError("Face detector is not loaded.")

        result = self.face_detector.detect(mp_image)
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
            "model": str(model_path(self.models_dir, FACE_DETECTOR_NAME)),
            "face_count": len(faces),
            "faces": faces,
        }
        write_json(output_dir / "face_box.json", payload)
        return payload

    def run_image_segmenter(self, mp_image: mp.Image, output_dir: Path) -> dict[str, Any]:
        if self.image_segmenter is None or self.models_dir is None:
            raise RuntimeError("Image segmenter is not loaded.")

        result = self.image_segmenter.segment(mp_image)
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
            "model": str(model_path(self.models_dir, IMAGE_SEGMENTER_NAME)),
            "mask_count": len(confidence_masks),
            "person_mask_index": person_mask_index,
            "alpha_path": str(alpha_path),
            "width": int(alpha.shape[1]),
            "height": int(alpha.shape[0]),
        }
        write_json(output_dir / "person_alpha.json", payload)
        return payload

    def run_face_landmarker(self, mp_image: mp.Image, output_dir: Path) -> dict[str, Any]:
        if self.face_landmarker is None or self.models_dir is None:
            raise RuntimeError("Face landmarker is not loaded.")

        result = self.face_landmarker.detect(mp_image)
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
                    "all_landmarks": all_landmarks_to_list(landmarks),
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
            "model": str(model_path(self.models_dir, FACE_LANDMARKER_NAME)),
            "face_count": len(faces),
            "pose_source": "facial_transformation_matrix_approx",
            "faces": faces,
        }
        write_json(output_dir / "face_pose.json", payload)
        return payload


def write_response(payload: dict[str, Any]) -> None:
    sys.stdout.write(json.dumps(payload, ensure_ascii=False, separators=(",", ":")) + "\n")
    sys.stdout.flush()


def main() -> int:
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    if hasattr(sys.stderr, "reconfigure"):
        sys.stderr.reconfigure(encoding="utf-8")

    worker = MediaPipeWorker()
    for line in sys.stdin:
        line = line.strip().lstrip("\ufeff")
        if not line:
            continue

        request_id: Any = None
        output_dir: Path | None = None
        try:
            command = json.loads(line)
            request_id = command.get("request_id")
            if command.get("command") == "shutdown":
                write_response({"request_id": request_id, "status": "ok", "run_mode": "worker", "message": "shutdown"})
                break

            output_value = command.get("output")
            output_dir = Path(str(output_value)) if output_value else None

            if command.get("command") == "warmup":
                write_response(worker.warmup(command))
                continue

            if command.get("command") != "run":
                raise RuntimeError("Unsupported command: " + str(command.get("command")))

            write_response(worker.run(command))
        except Exception as ex:
            payload = {
                "request_id": request_id,
                "status": "error",
                "engine": "MediaPipe",
                "run_mode": "worker",
                "error": str(ex),
                "traceback": traceback.format_exc(),
            }
            if output_dir is not None:
                output_dir.mkdir(parents=True, exist_ok=True)
                write_json(output_dir / "mediapipe_result.json", payload)
            write_response(payload)

    worker.unload()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
