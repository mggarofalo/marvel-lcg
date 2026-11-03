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
| `answering.svg` | A speech mark: this seat must answer the current decision. |
| `active_seat.svg` | A flag: it is this seat's turn. |
| `turn_order.svg` | Chevrons before a stop: each villain-phase step resolves for every area in turn order. |
| `public_focus.svg` | An eye: the table is showing this seat's public workspace. |
| `each_player.svg` | Two people: "each player" counts the players in the same game area. |
| `your_seat.svg` | A map pin: the seat you play. |
| `attack.svg` | A blade: an enemy attack is resolving. |
| `boost.svg` | A warning triangle: boost icons are not counted yet. |
| `sync_stale.svg` | A broken refresh loop: the displayed table may not match the authority. |
| `damage.svg` | A burst: the choice deals damage. |
| `remove_threat.svg` | A ring with a bar: the choice removes threat. |
| `draw.svg` | A card with a plus: the choice draws cards. |
| `stun.svg` | A jagged star, the stunned badge's shape: the choice stuns. |
| `confuse.svg` | Two rings, the confused badge's ring: the choice confuses. |
| `tough.svg` | An octagon, the tough badge's shape: the choice gives tough. |
| `heal.svg` | A cross: the choice heals damage. |
| `ready.svg` | An upright card with a lift: the choice readies. |
| `exhaust.svg` | A card turned sideways: the choice exhausts. |
| `place_threat.svg` | A ring with a plus, beside threat removal's bar: the choice places threat. |
| `move_threat.svg` | A ring with an arrow: the choice moves threat. |

Two pairs of facts share an icon because they state one concept. The lock marks both
an out-of-reach area and the rule that areas cannot affect one another, since that rule
is why an area is out of reach. The diamond marks stage 4A's pile: the stored Dominions,
the acceleration tokens waiting for stage 4A, and the completion that stores a Dominion
there. The villain-phase steps have no icon; they are drawn as the acting area's pips.

They contain no external art. `TableWording` names which icon states which fact.
