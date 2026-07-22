"""Build a smooth looping dark aurora GIF from keyframes."""
from __future__ import annotations

from pathlib import Path

from PIL import Image

ASSETS = Path(
    r"C:\Users\6xesh\.cursor\projects\c-Users-6xesh-Documents-scripts-vscode-Roblox\assets"
)
OUT_DIR = Path(
    r"c:\Users\6xesh\Documents\scripts\vscode\Roblox\Froststrap-2.0.0-beta.10\Froststrap\Assets"
)
W, H = 960, 540
BLENDS_BETWEEN = 4  # intermediate frames for video-like smoothness
DURATION_MS = 80


def main() -> None:
    frames_paths = sorted(ASSETS.glob("aurora-v2-*.png"))
    if len(frames_paths) < 2:
        frames_paths = sorted(ASSETS.glob("aurora-frame-*.png"))
    print("keyframes:", [p.name for p in frames_paths])
    if len(frames_paths) < 2:
        raise SystemExit("need keyframes")

    keys = [
        Image.open(p).convert("RGB").resize((W, H), Image.Resampling.LANCZOS)
        for p in frames_paths
    ]

    smooth: list[Image.Image] = []
    for i in range(len(keys)):
        a = keys[i]
        b = keys[(i + 1) % len(keys)]
        for s in range(BLENDS_BETWEEN):
            t = s / BLENDS_BETWEEN
            smooth.append(Image.blend(a, b, t))

    OUT_DIR.mkdir(parents=True, exist_ok=True)
    gif_path = OUT_DIR / "aurora-dark.gif"
    still_path = OUT_DIR / "aurora-dark-still.png"

    # Adaptive palette keeps neon without huge file size
    quantized = [
        im.convert("P", palette=Image.Palette.ADAPTIVE, colors=128) for im in smooth
    ]
    quantized[0].save(
        gif_path,
        save_all=True,
        append_images=quantized[1:],
        duration=DURATION_MS,
        loop=0,
        optimize=True,
        disposal=2,
    )
    smooth[0].save(still_path, optimize=True)
    print("wrote", gif_path, "frames", len(smooth), "mb", round(gif_path.stat().st_size / 1024 / 1024, 2))


if __name__ == "__main__":
    main()
