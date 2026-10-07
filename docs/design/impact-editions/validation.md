# Foundation validation

MARVEL-461, macOS, Godot 4.7.1 .NET, 1920×1080 project viewport.
The foundation commit containing this record is the reviewed source identity.
This is synthetic native primitive evidence, not a played Core scenario.

- Debug and Release Godot builds pass with warnings treated as errors.
- The normal commit gates pass source structure checks, the full Release build,
  all 3,732 managed tests, and the existing native smoke matrix. Focused tests
  cover markup, resource identity, canonical D/S/U, packaged assets, palette
  contrast and unchanged geometry/art contracts.
- The native `--marvel-b1-sample` loads actual embedded fonts, verifies glyph
  coverage and positive baseline metrics, constructs all five frame families,
  and decodes the actual resource SVG textures. Each texture must retain its
  semantic fill and an ink outline; antialiasing permits a bounded edge-color
  tolerance. No screenshot-existence assertion supplies this verdict.
- Initial native texture validation found that eight-digit HTML colors were
  not preserved by the SVG decoder. Six-digit SVG colors repair the actual
  resource and silhouette path. The native fill/outline assertions pass after
  repair, including the yellow resource on a light field.
- The specimen constructs 50%, 80%, 100% and 150% profiles without native errors.
  At width 156, the long-title sample measures 128×34 at 14px, 130×44 at 18px,
  and 155×105 at 29px. Final ascent/descent metrics with the explicit bundled
  fallback are 15/5, 20/6 and 31/9 respectively. These measurements guide the
  separate B1 face-geometry review; they do not certify a two-line title slot.
- The focused headless native input replay clicks 150% and activates 50% with
  Enter, retaining all five visible frame families. This caught an uninitialized
  gameplay-input callback in diagnostic startup; the specimen now disables that
  owner callback while retaining child GUI input. `B1_PRIMITIVES_INPUT_OK` passes
  after repair.
- Visible desktop pointer/keyboard review and visual captures were **not completed**:
  direct GUI startup aborted in the execution environment and the app-control
  tool explicitly rejected access to Godot. No alternate UI route was used.
- Packaged Windows rendering and independent uncoached engine-backed review
  remain pending. Headless macOS evidence cannot substitute for those verdicts.

Replay the bounded probe with a writable diagnostic log in restricted shells:

```sh
MARVEL_UI_SCALE=80 /path/to/Godot --headless --audio-driver Dummy \
  --log-file /private/tmp/marvel-b1-sample.log --path src/Marvel.Godot \
  --quit-after 3 -- --marvel-b1-sample
```

Expected completion: `B1_PRIMITIVES_OK synthetic=true`. Change the scale to
50, 100 or 150 for the other profiles. Local diagnostic logs are temporary
artifacts, not build inputs or repository dependencies.

The focused native input replay uses the same explicit synthetic entry:

```sh
/path/to/Godot --headless --audio-driver Dummy \
  --log-file /private/tmp/marvel-b1-sample-input.log --path src/Marvel.Godot \
  --script res://smoke/card_visual_sample_smoke.gd -- --marvel-b1-sample
```

Expected completion: `B1_PRIMITIVES_INPUT_OK pointer=150 keyboard=50 synthetic=true`.
