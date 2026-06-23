#!/usr/bin/env python
"""Persistent BiRefNet worker for KRetouch Studio."""

from __future__ import annotations

import contextlib
import gc
import json
import os
import sys
import time
import traceback
from pathlib import Path
from typing import Any

import torch
from PIL import Image
from torchvision import transforms
from transformers import AutoModelForImageSegmentation

from birefnet_helper import (
    DEFAULT_MODEL,
    DEFAULT_SIZE,
    DEFAULT_INPUT_SHARPEN,
    create_tensor_transform,
    create_transform,
    extract_prediction,
    prepare_model_input_image,
    save_debug_outputs,
    select_device,
    write_json,
)


class BiRefNetWorker:
    def __init__(self) -> None:
        self.model: torch.nn.Module | None = None
        self.model_id: str | None = None
        self.device_request: str | None = None
        self.device: torch.device | None = None
        self.dtype: torch.dtype | None = None
        self.transform_size: int | None = None
        self.transform_sharpen: int | None = None
        self.transform: transforms.Compose | None = None

    def ensure_loaded(self, model_id: str, size: int, input_sharpen: int, device_request: str) -> float:
        device = select_device(device_request)
        dtype = torch.float16 if device.type == "cuda" else torch.float32
        needs_model_load = (
            self.model is None
            or self.model_id != model_id
            or self.device_request != device_request
            or self.device != device
            or self.dtype != dtype
        )

        load_seconds = 0.0
        if needs_model_load:
            self.unload()
            load_started = time.perf_counter()
            with contextlib.redirect_stdout(sys.stderr):
                model = AutoModelForImageSegmentation.from_pretrained(
                    model_id,
                    trust_remote_code=True,
                    dtype=dtype,
                )
                model.to(device)
                model.eval()

            self.model = model
            self.model_id = model_id
            self.device_request = device_request
            self.device = device
            self.dtype = dtype
            load_seconds = time.perf_counter() - load_started

        if self.transform is None or self.transform_size != size or self.transform_sharpen != input_sharpen:
            self.transform = create_transform(size, input_sharpen)
            self.transform_size = size
            self.transform_sharpen = input_sharpen

        return load_seconds

    def unload(self) -> None:
        if self.model is not None:
            del self.model

        self.model = None
        self.model_id = None
        self.device_request = None
        self.device = None
        self.dtype = None
        gc.collect()
        if torch.cuda.is_available():
            torch.cuda.empty_cache()

    def run(self, command: dict[str, Any]) -> dict[str, Any]:
        request_id = command.get("request_id")
        image_path = Path(str(command.get("image", "")))
        output_dir = Path(str(command.get("output", "")))
        model_id = str(command.get("model") or DEFAULT_MODEL)
        size = int(command.get("size") or DEFAULT_SIZE)
        input_sharpen_value = command.get("input_sharpen")
        input_sharpen = DEFAULT_INPUT_SHARPEN if input_sharpen_value is None else int(input_sharpen_value)
        device_request = str(command.get("device") or "auto")
        prepared_input_value = command.get("prepared_input")
        prepared_input_path = Path(str(prepared_input_value)) if prepared_input_value else None

        output_dir.mkdir(parents=True, exist_ok=True)
        started = time.perf_counter()

        load_seconds = self.ensure_loaded(model_id, size, input_sharpen, device_request)
        if self.model is None or self.transform is None or self.device is None or self.dtype is None:
            raise RuntimeError("BiRefNet worker model is not loaded.")

        image = Image.open(image_path).convert("RGB")
        used_prepared_input_path: Path | None = None
        if prepared_input_path is not None and prepared_input_path.exists():
            model_input = Image.open(prepared_input_path).convert("RGB")
            if model_input.size != (size, size):
                model_input = prepare_model_input_image(image, size, input_sharpen)
            else:
                used_prepared_input_path = prepared_input_path

            input_tensor = create_tensor_transform()(model_input).unsqueeze(0).to(device=self.device, dtype=self.dtype)
        else:
            input_tensor = self.transform(image).unsqueeze(0).to(device=self.device, dtype=self.dtype)

        infer_started = time.perf_counter()
        with torch.inference_mode(), contextlib.redirect_stdout(sys.stderr):
            output = self.model(input_tensor)
            prediction = extract_prediction(output).sigmoid().detach().float().cpu()
        infer_seconds = time.perf_counter() - infer_started

        mask_tensor = prediction[0].squeeze().clamp(0.0, 1.0)
        mask = transforms.ToPILImage()(mask_tensor).resize(image.size, Image.Resampling.LANCZOS)
        alpha_path = output_dir / "person_alpha.png"
        mask.save(alpha_path)
        debug_paths = save_debug_outputs(image, mask, output_dir)

        payload: dict[str, Any] = {
            "request_id": request_id,
            "status": "ok",
            "engine": "BiRefNet",
            "run_mode": "worker",
            "worker_pid": os.getpid(),
            "model": model_id,
            "image": str(image_path),
            "image_size": {"width": image.width, "height": image.height},
            "inference_size": size,
            "input_sharpen": input_sharpen,
            "prepared_input_path": str(used_prepared_input_path) if used_prepared_input_path is not None else None,
            "device": str(self.device),
            "dtype": str(self.dtype),
            "load_seconds": round(load_seconds, 3),
            "infer_seconds": round(infer_seconds, 3),
            "total_seconds": round(time.perf_counter() - started, 3),
            "alpha_path": str(alpha_path),
            "debug_outputs": debug_paths,
        }
        write_json(output_dir / "person_alpha.json", payload)
        write_json(output_dir / "birefnet_result.json", payload)
        return payload

    def prepare_input(self, command: dict[str, Any]) -> dict[str, Any]:
        request_id = command.get("request_id")
        image_path = Path(str(command.get("image", "")))
        output_dir = Path(str(command.get("output", "")))
        size = int(command.get("size") or DEFAULT_SIZE)
        input_sharpen_value = command.get("input_sharpen")
        input_sharpen = DEFAULT_INPUT_SHARPEN if input_sharpen_value is None else int(input_sharpen_value)

        output_dir.mkdir(parents=True, exist_ok=True)
        started = time.perf_counter()

        image = Image.open(image_path).convert("RGB")
        prepared = prepare_model_input_image(image, size, input_sharpen)
        input_path = output_dir / f"birefnet_input_{size}.png"
        prepared.save(input_path)

        payload: dict[str, Any] = {
            "request_id": request_id,
            "status": "ok",
            "engine": "BiRefNet",
            "run_mode": "worker",
            "command": "prepare_input",
            "worker_pid": os.getpid(),
            "image": str(image_path),
            "image_size": {"width": image.width, "height": image.height},
            "inference_size": size,
            "input_sharpen": input_sharpen,
            "input_path": str(input_path),
            "total_seconds": round(time.perf_counter() - started, 3),
        }
        write_json(output_dir / "birefnet_input.json", payload)
        return payload

    def warmup(self, command: dict[str, Any]) -> dict[str, Any]:
        request_id = command.get("request_id")
        output_value = command.get("output")
        output_dir = Path(str(output_value)) if output_value else None
        model_id = str(command.get("model") or DEFAULT_MODEL)
        size = int(command.get("size") or DEFAULT_SIZE)
        input_sharpen_value = command.get("input_sharpen")
        input_sharpen = DEFAULT_INPUT_SHARPEN if input_sharpen_value is None else int(input_sharpen_value)
        device_request = str(command.get("device") or "auto")

        started = time.perf_counter()
        load_seconds = self.ensure_loaded(model_id, size, input_sharpen, device_request)
        if self.model is None or self.device is None or self.dtype is None:
            raise RuntimeError("BiRefNet worker model is not loaded.")

        payload: dict[str, Any] = {
            "request_id": request_id,
            "status": "ok",
            "engine": "BiRefNet",
            "run_mode": "worker",
            "command": "warmup",
            "worker_pid": os.getpid(),
            "model": model_id,
            "inference_size": size,
            "input_sharpen": input_sharpen,
            "device": str(self.device),
            "dtype": str(self.dtype),
            "load_seconds": round(load_seconds, 3),
            "total_seconds": round(time.perf_counter() - started, 3),
        }
        if output_dir is not None:
            output_dir.mkdir(parents=True, exist_ok=True)
            write_json(output_dir / "birefnet_warmup_result.json", payload)

        return payload


def write_response(payload: dict[str, Any]) -> None:
    sys.stdout.write(json.dumps(payload, ensure_ascii=False, separators=(",", ":")) + "\n")
    sys.stdout.flush()


def main() -> int:
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    if hasattr(sys.stderr, "reconfigure"):
        sys.stderr.reconfigure(encoding="utf-8")

    worker = BiRefNetWorker()
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

            if command.get("command") == "warmup":
                output_value = command.get("output")
                output_dir = Path(str(output_value)) if output_value else None
                write_response(worker.warmup(command))
                continue

            if command.get("command") == "prepare_input":
                output_value = command.get("output")
                output_dir = Path(str(output_value)) if output_value else None
                write_response(worker.prepare_input(command))
                continue

            if command.get("command") != "run":
                raise RuntimeError("Unsupported command: " + str(command.get("command")))

            output_value = command.get("output")
            output_dir = Path(str(output_value)) if output_value else None
            write_response(worker.run(command))
        except Exception as ex:
            payload = {
                "request_id": request_id,
                "status": "error",
                "engine": "BiRefNet",
                "run_mode": "worker",
                "error": str(ex),
                "traceback": traceback.format_exc(),
            }
            if output_dir is not None:
                output_dir.mkdir(parents=True, exist_ok=True)
                write_json(output_dir / "birefnet_result.json", payload)
            write_response(payload)

    worker.unload()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
