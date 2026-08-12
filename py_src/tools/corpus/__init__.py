"""Turning a working bot into a corpus.

`engine/device/manager/bot/` plays games. This package decides *which* games to
play, runs them across processes, and describes what came out well enough to do
it again. See MARVEL-15.

    inventory.py   what there is to sample from
    plan.py        which games to play, decided deterministically from a seed
    generate.py    play them, resumably, and say how it went

The split is not cosmetic. A plan is a pure function of (seed, sizes,
inventory), so it can be printed, diffed, committed and reproduced without
playing anything -- which is what makes "regenerate the identical corpus" a
checkable claim rather than a hope.
"""
