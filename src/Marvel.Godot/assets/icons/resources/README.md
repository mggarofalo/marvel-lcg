# Resource icons

These four SVG paths are outlines of P, M, E and W from the repository-pinned
`../../fonts/ChampionsIcons.ttf`. They use the same resource shapes as card
faces and printed rules. The font's provenance and license apply.

The SVGs fit each glyph's bounds with a 4% margin, invert the font Y axis and
store a white source fill. `ResourceIconRendering` applies the canonical
resource color from `CardVisualTokens` on both light and dark fields. Godot loads them from
embedded resources so exported and unimported development builds agree.
