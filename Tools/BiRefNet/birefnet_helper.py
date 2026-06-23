#!/usr/bin/env python
"""Generate a person alpha matte with BiRefNet for KRetouch Studio."""

from __future__ import annotations

import argparse
import json
import time
import traceback
from pathlib import Path
from typing import Any

import torch
from PIL import Image, ImageFilter
from torchvision import transforms
from transformers import AutoModelForImageSegmentation


DEFAULT_MODEL = "ZhengPeng7/BiRefNet_lite-matting"
DEFAULT_SIZE = 1024
DEFAULT_INPUT_SHARPEN = 100


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="KRetouch Studio BiRefNet helper")
    parser.add_argument("--image", required=True, help="Input image path")
    parser.add_argument("--output", required=True, help="Output artifact folder")
    parser.add_argument("--model", default=DEFAULT_MODEL, help="Hugging Face model id")
    parser.add_argument("--size", type=int, default=DEFAULT_SIZE, help="Square inference size")
    parser.add_argument("--input-sharpen", type=int, default=DEFAULT_INPUT_SHARPEN, help="Sharpen strength after resize")
    parser.add_argument("--device", choices=("auto", "cuda", "cpu"), default="auto")
    return parser.parse_args()


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")


def select_device(requested_device: str) -> torch.device:
    if requested_device == "cuda":
        if not torch.cuda.is_available():
            raise RuntimeError("CUDA was requested but is not available.")
        return torch.device("cuda")

    if requested_device == "cpu":
        return torch.device("cpu")

    return torch.device("cuda" if torch.cuda.is_available() else "cpu")


def sharpen_for_model_input(image: Image.Image, strength: int) -> Image.Image:
    strength = max(0, min(100, int(strength)))
    if strength <= 0:
        return image

    return image.filter(ImageFilter.UnsharpMask(radius=1.0, percent=strength, threshold=0))


def create_transform(size: int, input_sharpen: int = DEFAULT_INPUT_SHARPEN) -> transforms.Compose:
    return transforms.Compose(
        [
            transforms.Resize((size, size)),
            transforms.Lambda(lambda image: sharpen_for_model_input(image, input_sharpen)),
            transforms.ToTensor(),
            transforms.Normalize([0.485, 0.456, 0.406], [0.229, 0.224, 0.225]),
        ]
    )


def extract_prediction(output: Any) -> torch.Tensor:
    if isinstance(output, torch.Tensor):
        return output

    if isinstance(output, dict):
        for key in ("logits", "pred", "prediction"):
            value = output.get(key)
            if isinstance(value, torch.Tensor):
                return value

        for value in output.values():
            if isinstance(value, torch.Tensor):
                return value
            if isinstance(value, (list, tuple)) and value:
                return extract_prediction(value)

    if isinstance(output, (list, tuple)) and output:
        return extract_prediction(output[-1])

    raise RuntimeError(f"Unsupported BiRefNet output type: {type(output)!r}")


def save_debug_outputs(image: Image.Image, mask: Image.Image, output_dir: Path) -> dict[str, str]:
    foreground = image.convert("RGBA")
    foreground.putalpha(mask)

    white = Image.new("RGBA", image.size, (255, 255, 255, 255))
    white.alpha_composite(foreground)

    foreground_path = output_dir / "debug_birefnet_foreground.png"
    white_path = output_dir / "debug_birefnet_white.png"
    foreground.save(foreground_path)
    white.convert("RGB").save(white_path, quality=100)
    return {
        "foreground": str(foreground_path),
        "white": str(white_path),
    }


def run(args: argparse.Namespace) -> dict[str, Any]:
    image_path = Path(args.image)
    output_dir = Path(args.output)
    output_dir.mkdir(parents=True, exist_ok=True)

    started = time.perf_counter()
    device = select_device(args.device)
    dtype = torch.float16 if device.type == "cuda" else torch.float32

    image = Image.open(image_path).convert("RGB")
    input_tensor = create_transform(args.size, args.input_sharpen)(image).unsqueeze(0).to(device=device, dtype=dtype)

    load_started = time.perf_counter()
    model = AutoModelForImageSegmentation.from_pretrained(
        args.model,
        trust_remote_code=True,
        dtype=dtype,
    )
    model.to(device)
    model.eval()
    load_seconds = time.perf_counter() - load_started

    infer_started = time.perf_counter()
    with torch.inference_mode():
        output = model(input_tensor)
        prediction = extract_prediction(output).sigmoid().detach().float().cpu()
    infer_seconds = time.perf_counter() - infer_started

    mask_tensor = prediction[0].squeeze().clamp(0.0, 1.0)
    mask = transforms.ToPILImage()(mask_tensor).resize(image.size, Image.Resampling.LANCZOS)
    alpha_path = output_dir / "person_alpha.png"
    mask.save(alpha_path)
    debug_paths = save_debug_outputs(image, mask, output_dir)

    payload: dict[str, Any] = {
        "status": "ok",
        "engine": "BiRefNet",
        "model": args.model,
        "image": str(image_path),
        "image_size": {"width": image.width, "height": image.height},
        "inference_size": args.size,
        "input_sharpen": args.input_sharpen,
        "device": str(device),
        "dtype": str(dtype),
        "load_seconds": round(load_seconds, 3),
        "infer_seconds": round(infer_seconds, 3),
        "total_seconds": round(time.perf_counter() - started, 3),
        "alpha_path": str(alpha_path),
        "debug_outputs": debug_paths,
    }
    write_json(output_dir / "person_alpha.json", payload)
    write_json(output_dir / "birefnet_result.json", payload)
    return payload


def main() -> int:
    args = parse_args()
    output_dir = Path(args.output)
    try:
        payload = run(args)
        print(json.dumps(payload, ensure_ascii=False, indent=2))
        return 0
    except Exception as ex:
        payload = {
            "status": "error",
            "engine": "BiRefNet",
            "error": str(ex),
            "traceback": traceback.format_exc(),
        }
        write_json(output_dir / "birefnet_result.json", payload)
        print(json.dumps(payload, ensure_ascii=False, indent=2))
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
