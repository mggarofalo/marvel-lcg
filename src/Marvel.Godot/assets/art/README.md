# Original B1 silhouette art

These editable, code-native SVG outlines are original drawings from the approved
7 October 2026 Impact Editions design package. They contain no scanned artwork,
remote references, fonts or card text. The drawing source is retained in the
[approved vector reference](../../../../docs/design/impact-editions/reference/).
`fallback.svg` is the generic shield drawing. Numbered files identify visible
Core faces for asset lookup, not a runtime card rule.

The files are embedded in the Godot assembly. `BuiltInCardArt` supplies this
fallback through `ICardArtProvider`; a configured lawful local art pack takes
precedence. No online acquisition is part of rendering or building.
