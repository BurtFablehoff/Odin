"""Contact sheet of the bases and style references the v12 Codex art briefs point at.
Writes local-data/reference-review/v12/brief-bases.png. Read-only on its inputs."""
import os, sys
from PIL import Image, ImageDraw
root = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
items = [
    ("sit/000 (original)", "art/frames/sit/000.png"),
    ("lay/000 (original)", "art/frames/lay/000.png"),
    ("loaf/000 (original)", "art/frames/loaf/000.png"),
    ("rear base (approved v1)", "draft-assets/approved-sitting-v1/rear.png"),
    ("lookback base (approved v1)", "draft-assets/approved-sitting-v1/lookback.png"),
    ("side base (approved v1)", "draft-assets/approved-sitting-v1/side.png"),
    ("tucked (approved v1, parked)", "draft-assets/approved-tucked-sleep-v1/tucked.png"),
    ("runtime rear (restore-v11)", "draft-assets/restore-v11/rear.png"),
    ("runtime side (restore-v11)", "draft-assets/restore-v11/side.png"),
    ("runtime lookback (numbered-sizing-v2)", "draft-assets/numbered-sizing-v2/sit-lookback.png"),
]
cell = 420; cols = 5; rows = (len(items) + cols - 1) // cols
sheet = Image.new("RGB", (cols * cell, rows * (cell + 28)), (235, 238, 242))
draw = ImageDraw.Draw(sheet)
for i, (label, rel) in enumerate(items):
    im = Image.open(os.path.join(root, rel)).convert("RGBA")
    w, h = im.size
    s = (cell - 10) / max(w, h)
    im = im.resize((int(w * s), int(h * s)), Image.LANCZOS)
    x = (i % cols) * cell; y = (i // cols) * (cell + 28)
    bg = Image.new("RGBA", (cell, cell), (200, 205, 212, 255))
    bg.alpha_composite(im, ((cell - im.width) // 2, (cell - im.height) // 2))
    sheet.paste(bg.convert("RGB"), (x, y))
    draw.text((x + 6, y + cell + 6), f"{label}  {w}x{h}", fill=(20, 20, 20))
out = os.path.join(root, "local-data/reference-review/v12/brief-bases.png")
sheet.save(out); print(out, sheet.size)
