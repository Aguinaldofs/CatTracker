from __future__ import annotations

from collections import deque
from pathlib import Path
from PIL import Image


ROOT = Path(__file__).resolve().parents[2]
SOURCE_DIR = ROOT / "ori"
ASSET_DIR = ROOT / "BongoCatTracker" / "Assets"

FILES = [
    ("image.png", "cat_idle.png"),
    ("image copy 2.png", "cat_left.png"),
    ("image copy 3.png", "cat_right.png"),
    ("image copy 4.png", "cat_both.png"),
]


def looks_like_checker(pixel: tuple[int, int, int, int]) -> bool:
    r, g, b, _ = pixel
    nearly_gray = max(r, g, b) - min(r, g, b) <= 10
    light_square = 238 <= r <= 248 and 238 <= g <= 248 and 238 <= b <= 248
    white_square = 250 <= r <= 255 and 250 <= g <= 255 and 250 <= b <= 255
    return nearly_gray and (light_square or white_square)


def remove_checkerboard_background(img: Image.Image) -> Image.Image:
    img = img.convert("RGBA")
    pixels = img.load()
    width, height = img.size

    seen: set[tuple[int, int]] = set()
    queue: deque[tuple[int, int]] = deque()

    for x in range(width):
        queue.append((x, 0))
        queue.append((x, height - 1))
    for y in range(height):
        queue.append((0, y))
        queue.append((width - 1, y))

    while queue:
        x, y = queue.popleft()
        if (x, y) in seen:
            continue
        seen.add((x, y))

        r, g, b, a = pixels[x, y]
        if not looks_like_checker((r, g, b, a)):
            continue

        pixels[x, y] = (r, g, b, 0)

        if x > 0:
            queue.append((x - 1, y))
        if x < width - 1:
            queue.append((x + 1, y))
        if y > 0:
            queue.append((x, y - 1))
        if y < height - 1:
            queue.append((x, y + 1))

    # Clear antialiased checker edges without touching opaque white fur.
    for y in range(height):
        for x in range(width):
            r, g, b, a = pixels[x, y]
            if a == 0 or not (235 <= r <= 255 and 235 <= g <= 255 and 235 <= b <= 255):
                continue

            has_transparent_neighbor = False
            for nx in range(max(0, x - 1), min(width, x + 2)):
                for ny in range(max(0, y - 1), min(height, y + 2)):
                    if pixels[nx, ny][3] == 0:
                        has_transparent_neighbor = True
                        break
                if has_transparent_neighbor:
                    break

            if has_transparent_neighbor and max(r, g, b) - min(r, g, b) <= 12:
                pixels[x, y] = (r, g, b, 0)

    return img


def main() -> None:
    ASSET_DIR.mkdir(parents=True, exist_ok=True)
    for source_name, output_name in FILES:
        source = SOURCE_DIR / source_name
        if not source.exists():
            print(f"missing: {source}")
            continue

        img = Image.open(source)
        img = remove_checkerboard_background(img)
        img.save(ASSET_DIR / output_name)
        print(f"wrote: {ASSET_DIR / output_name}")


if __name__ == "__main__":
    main()
