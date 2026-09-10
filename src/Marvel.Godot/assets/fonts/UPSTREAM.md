# Champions icon font

| | |
|---|---|
| Upstream | https://github.com/zzorba/marvelsdb |
| Commit | `991193c11f9d4e2057253f427c65d9055da9a3b1` |
| Original path | `src/AppBundle/Resources/public/fonts/ChampionsIcons.ttf` |
| Pinned file | https://github.com/zzorba/marvelsdb/blob/991193c11f9d4e2057253f427c65d9055da9a3b1/src/AppBundle/Resources/public/fonts/ChampionsIcons.ttf |
| Local path | `src/Marvel.Godot/assets/fonts/ChampionsIcons.ttf` |

## Embedded metadata

The embedded TrueType metadata identifies the copyright as
`Copyright (c) 2020, Hitch`, the family as `Champions Icons`, and the version as
`001.000`. The metadata does not include redistribution terms.

## Provenance research

[MarvelSDB history](https://github.com/zzorba/marvelsdb/commit/6708dcc5024111ecca4f641bd53a450b593aeb8d)
first identifies this path in community commit
`6708dcc5024111ecca4f641bd53a450b593aeb8d` on 2024-12-31, titled
`add amplify icon and support for scheme_amplify`. Later MarvelSDB commits
modify the file. The repository history provides no evidence that the font
comes from Fantasy Flight Games.

The [official Fantasy Flight Games Marvel Champions product and support
inventory](https://www.fantasyflightgames.com/en/products/marvel-champions-the-card-game/)
contains no fan kit, design kit, or icon font. [Hall of
Heroes](https://hallofheroeslcg.com/custom-content/) lists text fonts but does
not list `ChampionsIcons.ttf`; Hall of Heroes is not an official source.

The separate `Marvel LCG Glyphs Font v1.ttf`, available through the
[BoardGameGeek Marvel Champions files
inventory](https://boardgamegeek.com/boardgame/285774/marvel-champions-the-card-game/files),
is uploaded by BoardGameGeek user `i.a.m` / Fred Methot under BGG license ID 1,
All Rights Reserved. Its SHA-256 is
`64a2a3e6dc97844e72c64c8f4db1ab2eeb56b7d2a961dff1ab604eaee4683646`.
It is a different binary and is not used by this project.

At the pinned commit, MarvelSDB contains no license file or font-specific
license grant. No official source or redistribution grant is found for
`ChampionsIcons.ttf`; release redistribution remains unresolved. This file
records provenance and research findings without making a license claim.

The Godot assembly embeds the local file under
`Marvel.Godot.Assets.ChampionsIcons.ttf`. At runtime, the exact embedded bytes
back the cached `res://assets/fonts/ChampionsIcons.runtime.tres` font resource
used by compact cards and inspector markup.
