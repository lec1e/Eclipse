"""Assemble dark aurora PNG frames into a looping GIF for Eclipse."""
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


def main() -> None:
    frames_paths = sorted(ASSETS.glob("aurora-frame-*.png"))
    print("frames:", [p.name for p in frames_paths])
    if len(frames_paths) < 2:
        raise SystemExit("need at least 2 frames")

    imgs: list[Image.Image] = []
    for p in frames_paths:
        im = Image.open(p).convert("RGB").resize((W, H), Image.Resampling.LANCZOS)
        imgs.append(im)

    # Blend midpoints for a smoother loop
    smooth: list[Image.Image] = []
    for i in range(len(imgs)):
        a = imgs[i]
        b = imgs[(i + 1) % len(imgs)]
        smooth.append(a)
        smooth.append(Image.blend(a, b, 0.33))
        smooth.append(Image.blend(a, b, 0.66))

    OUT_DIR.mkdir(parents=True, exist_ok=True)
    gif_path = OUT_DIR / "aurora-dark.gif"
    still_path = OUT_DIR / "aurora-dark-still.png"

    # Quantize for smaller GIF while keeping neon look
    quantized = [im.convert("P", palette=Image.Palette.ADAPTIVE, colors=128) for im in smooth]
    quantized[0].save(
        gif_path,
        save_all=True,
        append_images=quantized[1:],
        duration=80,
        loop=0,
        optimize=True,
        disposal=2,
    )
    smooth[0].save(still_path)
    print("wrote", gif_path, "size_mb", round(gif_path.stat().st_size / 1024 / 1024, 2))
    print("still", still_path)


if __name__ == "__main__":
    main()
