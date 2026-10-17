"""Export CUSTOMIZING's four user-assigned states per tag without resizing.

Limited RGBA decoder: https://github.com/aseprite/aseprite/blob/main/docs/ase-file-specs.md
Usage: python export_applicant_aseprite.py --input FILE --output DIRECTORY
Frame indices in characters.json are zero-based; state names are user-defined.
"""
import argparse
import hashlib
import json
from pathlib import Path
import struct
import sys
import zlib

from PIL import Image

TAGS = ('FOOL MAGICIAN HIGHPRIESTESS EMPRESS EMPEROR HIEROPHANT LOVERS '
        'CHARIOT JUSTICE HERMIT WHEELOFFORTUNE STRENGTH HANGEDMAN').split()
STATES = ('idle', 'talk', 'accepted', 'rejected')


def require(condition, message):
    if not condition:
        raise ValueError(message)


class Reader:
    def __init__(self, data):
        self.data, self.pos = data, 0

    def take(self, count):
        require(0 <= count <= len(self.data) - self.pos, 'Truncated structure')
        result = self.data[self.pos:self.pos + count]
        self.pos += count
        return result

    def unpack(self, fmt):
        return struct.unpack('<' + fmt, self.take(struct.calcsize('<' + fmt)))

    def string(self):
        return self.take(self.unpack('H')[0]).decode('utf-8')

    def done(self):
        require(self.pos == len(self.data), 'Unexpected trailing structure bytes')


def decode(data):
    require(len(data) >= 128, 'Truncated file header')
    size, magic, count, width, height, depth, flags = struct.unpack_from('<I5HI', data)
    require(size == len(data) and magic == 0xA5E0, 'Invalid file header/size')
    require((count, width, height, depth) == (52, 57, 66, 32), 'Expected 52 RGBA frames of 57x66')
    require(flags & ~7 == 0, 'Unsupported header flags')
    layers, frames, tags, durations = [], [], [], []
    counts = {0: 0, 1: 0, 2: 0}
    offset = 128
    # These chunks do not affect compositing literal RGBA pixel samples.
    metadata = {0x0004, 0x0011, 0x2019, 0x2020, 0x2022}
    for frame_index in range(count):
        require(offset + 16 <= size, 'Truncated frame header')
        length, marker, old_count, duration, new_count = struct.unpack_from('<IHHH2xI', data, offset)
        end = offset + length
        require(marker == 0xF1FA and length >= 16 and end <= size, 'Invalid frame bounds')
        require(duration == 100, 'Expected 100ms frames')
        durations.append(duration)
        cursor, cels = offset + 16, {}
        for _ in range(new_count or old_count):
            require(cursor + 6 <= end, 'Truncated chunk header')
            chunk_size, kind = struct.unpack_from('<IH', data, cursor)
            require(chunk_size >= 6 and cursor + chunk_size <= end, 'Invalid chunk bounds')
            r = Reader(data[cursor + 6:cursor + chunk_size])
            if kind == 0x2004:
                require(frame_index == 0, 'Unsupported animated layer layout')
                lf, lt, child, _, _, blend, opacity = r.unpack('6HB')
                r.take(3)
                name = r.string()
                if flags & 4:
                    r.take(16)
                r.done()
                require(lt == 0 and child == 0 and blend == 0 and opacity == 255,
                        'Unsupported group/tilemap/blend/layer opacity')
                require(lf & ~0x7F == 0 and not lf & 64, 'Unsupported layer flags/reference layer')
                layers.append((name, bool(lf & 1)))
            elif kind == 0x2005:
                layer, x, y, opacity, cel_type, z_index = r.unpack('HhhBHh')
                r.take(5)
                require(layer < len(layers) and layer not in cels, 'Invalid/duplicate cel layer')
                require(opacity == 255 and z_index == 0, 'Unsupported cel opacity/z-index')
                require(cel_type in counts, 'Unsupported tilemap/cel type')
                counts[cel_type] += 1
                if cel_type == 1:
                    reference = r.unpack('H')[0]
                    require(reference < count, 'Linked frame out of bounds')
                    payload = reference
                    r.done()
                else:
                    cw, ch = r.unpack('HH')
                    require(0 < cw <= 65535 and 0 < ch <= 65535 and cw * ch <= 16777216,
                            'Invalid/oversized cel dimensions')
                    raw = r.take(len(r.data) - r.pos)
                    expected = cw * ch * 4
                    if cel_type == 2:
                        inflater = zlib.decompressobj()
                        raw = inflater.decompress(raw, expected + 1)
                        require(inflater.eof and not inflater.unused_data and not inflater.unconsumed_tail,
                                'Invalid compressed cel stream')
                    require(len(raw) == expected, 'Incorrect cel pixel byte count')
                    payload = Image.frombytes('RGBA', (cw, ch), raw)
                cels[layer] = (x, y, cel_type, payload)
            elif kind == 0x2018:
                tag_count = r.unpack('H')[0]
                r.take(8)
                for _ in range(tag_count):
                    first, last, direction, repeat = r.unpack('HHBH')
                    r.take(10)
                    tags.append((r.string(), first, last))
                    require(direction == 0, 'Unsupported tag direction')
                r.done()
            elif kind == 0x2007:
                profile_type, profile_flags, gamma = r.unpack('HHI')
                r.take(8)
                require(profile_type in (0, 1) and profile_flags == 0,
                        'Unsupported ICC/gamma color profile')
                r.done()
            elif kind not in metadata:
                raise ValueError(f'Unsupported chunk 0x{kind:04x}')
            cursor += chunk_size
        require(cursor == end, 'Frame chunk count/size mismatch')
        frames.append(cels)
        offset = end
    require(offset == size, 'Trailing file bytes')
    require(layers == [('Bckground', False), ('BODY', True), ('HEAD', True),
                       ('HAIR', True), ('FACE', True)], 'Unexpected layer layout/visibility')
    require(tags == [(tag, i * 4, i * 4 + 3) for i, tag in enumerate(TAGS)],
            'Unexpected tag order/ranges')
    cache = {}

    def image_for(frame, layer, active):
        key = (frame, layer)
        require(key not in active, 'Linked cel cycle')
        require(layer in frames[frame], 'Linked cel target missing')
        if key not in cache:
            cel = frames[frame][layer]
            cache[key] = (image_for(cel[3], layer, active | {key})
                          if cel[2] == 1 else cel[3])
        return cache[key]

    # Resolve invisible cels too, so malformed links always fail before writing.
    for frame, cels in enumerate(frames):
        for layer in cels:
            image_for(frame, layer, set())
    rendered = []
    for frame, cels in enumerate(frames):
        canvas = Image.new('RGBA', (width, height))
        for layer, (_, visible) in enumerate(layers):
            if visible and layer in cels:
                x, y = cels[layer][:2]
                canvas.alpha_composite(image_for(frame, layer, set()), dest=(x, y))
        rendered.append(canvas)
    return rendered, tags, counts


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--input', required=True, type=Path)
    parser.add_argument('--output', required=True, type=Path)
    args = parser.parse_args()
    source, output = args.input.resolve(), args.output.resolve()
    require(source.is_file(), 'Input file missing')
    require(source != output and source.parent != output, 'Output must be separate from input directory')
    data = source.read_bytes()
    images, tags, counts = decode(data)
    digest = hashlib.sha256(data).hexdigest()
    characters = []
    for index, (tag, first, last) in enumerate(tags, 1):
        identity = f'character-{index:02d}'
        states = []
        for state, frame in zip(STATES, range(first, last + 1)):
            relative = f'{identity}/{state}.png'
            target = output / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            images[frame].save(target, format='PNG')
            states.append({'state': state, 'sourceframe': frame, 'path': relative})
        characters.append({'id': identity, 'tag': tag, 'states': states})
    copied = output / 'Source' / 'CUSTOMIZING.aseprite'
    copied.parent.mkdir(parents=True, exist_ok=True)
    if copied.resolve() != source:
        copied.write_bytes(data)
    manifest = {'source': 'Source/CUSTOMIZING.aseprite', 'sourcehash': digest,
                'hashalgorithm': 'sha256', 'dimensions': {'width': 57, 'height': 66},
                'sourceframe_index_base': 0, 'frame_duration_ms': 100,
                'state_assignment': 'User-defined order within each four-frame tag',
                'characters': characters}
    (output / 'characters.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
    print(f'Exported 52 PNGs (57x66 RGBA), 13 characters; source copy + manifest; cels={counts}; unsupported=none; sha256={digest}')


if __name__ == '__main__':
    try:
        main()
    except (ValueError, OSError, struct.error, zlib.error) as error:
        print(f'Export failed: {error}', file=sys.stderr)
        sys.exit(1)