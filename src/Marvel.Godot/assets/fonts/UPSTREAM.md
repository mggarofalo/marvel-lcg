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

# Card typography

Barlow Regular, Bold and Italic provide printed rules; Barlow Condensed Bold provides card titles and large values. These fonts are vendored from the Google Fonts repository at commit `7085eb89a950e85db5b166b7a58d414544b4140c`, under `ofl/barlow/` and `ofl/barlowcondensed/`. Copyright 2017 The Barlow Project Authors. The accompanying [SIL Open Font License](Barlow-OFL.txt) permits redistribution. Runtime loading uses embedded bytes and requires no network or Godot import scan.

Pinned source: https://github.com/google/fonts/tree/7085eb89a950e85db5b166b7a58d414544b4140c/ofl/barlow

The canonical symbol character assignments are pinned with the font in [icons.css](https://github.com/zzorba/marvelsdb/blob/991193c11f9d4e2057253f427c65d9055da9a3b1/src/AppBundle/Resources/public/css/icons.css): P physical, E energy, M mental, W wild, G per hero, U unique, S special star, D consequential damage (the `[cost]` token), B boost, A acceleration, F amplify, C crisis, H hazard. These are display assignments, not gameplay rules.

# Portable UI symbols

DejaVu Sans 2.37 is the embedded fallback for symbols absent from Barlow: arrows,
pile shapes, playback, selection, and status markers. Both the primary fonts
and this fallback disable system-font fallback. Text remains Barlow; canonical
card/resource icons retain their separate existing assets.

The unmodified `DejaVuSans.ttf` comes from the official release archive:
https://github.com/dejavu-fonts/dejavu-fonts/releases/download/version_2_37/dejavu-fonts-ttf-2.37.tar.bz2

Archive SHA-256 (also published at https://dejavu-fonts.github.io/Download.html):
`fa9ca4d13871dd122f61258a80d01751d603b4d3ee14095d65453b4e846e17d7`.
Font SHA-256:
`7da195a74c55bef988d0d48f9508bd5d849425c1770dba5d7bfc6ce9ed848954`.

The accompanying [license](DejaVu-LICENSE.txt) preserves the Bitstream copyright
and permission notice; DejaVu's changes are public domain. The font may be
redistributed with the application under those terms. Runtime loading uses
embedded bytes, with no platform font, import scan, or network dependency.

The fallback also participates in native line metrics. Compact progress labels
are checked for a visible line and nonempty character bounds on canonical
specimens and actual engine-backed boards. A native shaping/raster probe covers
all non-ASCII UI symbols in each typography role without system fallback.
