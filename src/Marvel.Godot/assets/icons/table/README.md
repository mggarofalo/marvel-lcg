# Table state icons

Original functional outlines for the table's state facts, drawn for this client on a
24-unit grid with a 2-unit white stroke, round caps and joins, and no fill. They follow
`../card/exhausted.svg`. The renderer tints them with the palette; shape, never color,
distinguishes one fact from another.

| File | Fact |
|---|---|
| `out_of_reach.svg` | A lock: the inspected seat's cards cannot affect this area. |
| `acting.svg` | A play mark: the villain phase is resolving here. |
| `departing.svg` | An arrow leaving a wall: this group leaves at the end of the phase. |
| `destination.svg` | An arrow meeting a wall: the leaving group can join this area. |
| `retained.svg` | An hourglass: an eliminated player's components wait here. |
| `shared.svg` | Two overlapping rings: every area shares this. |
| `your_area.svg` | A person: the inspected seat plays here. |
| `dominion.svg` | A diamond: Kang's Dominions stored under stage 4A. |
| `rules.svg` | An information mark: the rules that apply now. |

They contain no external art. `TableWording` names which icon states which fact.
