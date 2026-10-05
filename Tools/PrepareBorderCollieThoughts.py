"""Package the approved ImageGen strips into matching transparent GIFs and Unity frames.

No redraw or invented motion: split six source cells, use a shared scale within each
strip, align bottom-center, and encode the same keyframes for preview and runtime.
"""
from pathlib import Path
import json
from PIL import Image, ImageDraw, ImageFilter
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'SourceAssets' / 'BorderCollie'
OUTPUT = ROOT / 'Assets' / 'Sleepet' / 'Resources' / 'BorderCollieThoughts'
GIFS = SOURCE / 'Gifs'
NAMES = ('bark', 'licking1', 'licking2', 'itching', 'stretching', 'sitting')
SIZE, CONTENT, FLOOR = 256, 228, 244

def characters(source):
    # Generated cells can have unequal margins. Extract whole alpha-connected
    # characters instead of cutting a tail at a nominal cell boundary.
    mask = np.asarray(source.getchannel('A')) > 24
    parents, spans, previous = [], [], []
    def root(n):
        while parents[n] != n:
            parents[n] = parents[parents[n]]
            n = parents[n]
        return n
    for y, row in enumerate(mask):
        edges = np.flatnonzero(np.diff(np.r_[False, row, False].astype(np.int8)))
        current = []
        for left, right in zip(edges[::2], edges[1::2]):
            label = len(parents); parents.append(label)
            for a, b, old in previous:
                if a < right and b > left:
                    parents[root(old)] = root(label)
            current.append((left, right, label)); spans.append((y, left, right, label))
        previous = current
    groups = {}
    for y, left, right, label in spans:
        groups.setdefault(root(label), []).append((y, int(left), int(right)))
    groups = sorted(groups.values(), key=lambda g: sum(b-a for _,a,b in g), reverse=True)[:6]
    if len(groups) != 6 or min(sum(b-a for _,a,b in g) for g in groups) < 10000:
        raise ValueError('Expected six separate full-body characters')
    groups.sort(key=lambda g: min(a for _,a,_ in g))
    parts = []
    for group in groups:
        selection = Image.new('L', source.size); pen = ImageDraw.Draw(selection)
        for y, left, right in group: pen.line((left, y, right-1, y), fill=255)
        selection = selection.filter(ImageFilter.MaxFilter(5))
        alpha = np.minimum(np.asarray(source.getchannel('A')), np.asarray(selection))
        part = source.copy(); part.putalpha(Image.fromarray(alpha))
        parts.append(part.crop(part.getbbox()))
    return parts

def prepare():
    GIFS.mkdir(parents=True, exist_ok=True)
    preview = Image.new('RGB', (6 * SIZE, 6 * (SIZE + 30)), '#e9f3fa')
    draw = ImageDraw.Draw(preview)
    report = []
    all_frames = []
    for row, name in enumerate(NAMES):
        source = Image.open(SOURCE / (name + '-strip.png')).convert('RGBA')
        parts = characters(source)
        scale = min(CONTENT / max(p.width for p in parts), CONTENT / max(p.height for p in parts))
        target = OUTPUT / name
        target.mkdir(parents=True, exist_ok=True)
        frames = []
        for i, part in enumerate(parts):
            frame = Image.new('RGBA', (SIZE, SIZE))
            part = part.resize((round(part.width * scale), round(part.height * scale)), Image.Resampling.LANCZOS)
            frame.alpha_composite(part, ((SIZE - part.width) // 2, FLOOR - part.height))
            frame.save(target / f'{i:02}.png')
            frames.append(frame)
            preview.paste(frame, (i * SIZE, row * (SIZE + 30) + 30), frame)
        # Reserve palette index 255 for alpha; GIF supports binary transparency.
        indexed = []
        for frame in frames:
            quantized = frame.convert('RGB').quantize(colors=255, method=Image.Quantize.MEDIANCUT)
            alpha = frame.getchannel('A')
            quantized.paste(255, mask=alpha.point(lambda a: 255 if a < 100 else 0))
            quantized.info['transparency'] = 255
            indexed.append(quantized)
        indexed[0].save(GIFS / f'{name}.gif', save_all=True, append_images=indexed[1:],
                        duration=200, loop=0, disposal=2, transparency=255, optimize=False)
        with Image.open(GIFS / f'{name}.gif') as check:
            assert check.n_frames == 6 and check.size == (SIZE, SIZE)
        assert len({f.tobytes() for f in frames}) >= 4
        all_frames.append(frames)
        draw.text((12, row * (SIZE + 30) + 8), name, fill='#08254a')
        report.append({'motion': name, 'frames': 6, 'fps': 5, 'durationSeconds': 1.2, 'size': SIZE})
    preview.save(SOURCE / 'preview-sheet.jpg', quality=92)
    overview = []
    for index in range(6):
        grid = Image.new('RGB', (SIZE * 3, (SIZE + 30) * 2), '#e9f3fa')
        captions = ImageDraw.Draw(grid)
        for number, frames in enumerate(all_frames):
            x, y = number % 3 * SIZE, number // 3 * (SIZE + 30)
            captions.text((x + 14, y + 8), NAMES[number], fill='#08254a', font_size=18)
            grid.paste(frames[index], (x, y + 30), frames[index])
        overview.append(grid)
    overview[0].save(SOURCE / 'preview.gif', save_all=True, append_images=overview[1:], duration=200, loop=0)
    (SOURCE / 'manifest.json').write_text(json.dumps(report, indent=2), encoding='utf8')
    print('Verified six transparent GIFs and 36 matching Unity PNG frames.')

if __name__ == '__main__':
    prepare()
