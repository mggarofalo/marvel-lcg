# Care for Cassie actual card

The canonical `12025` row follows `datasets/cards/cards.json`. Printed GiveTo
routes it to Scott Lang even when another player reveals it. Scott may change to
alter-ego, then either pay the exhaust arrow cost to remove the obligation or
choose and discard a hand card, receive the next-own-turn prohibition, and
discard the obligation. The second branch has no arrow or dependent "then": an
empty hand does not prevent the remaining effect (`rr:ability.7.1`). The first
branch preserves the existing dependent exhaust/remove pattern.

The reviewed MARVEL-884 duration owns timing and form-change permission. This
row adds no engine behavior. All 754 previous ability objects and their raw
prefix are preserved; the book now has 755 rows and 32 authored Ant-Man faces.
The pack remains incomplete and publicly excluded.

Prepared actual-card tests use distinct Spider-Man/Ant-Man identities, routing
to non-first Scott from both hero forms, optional flip and removal, exhausted
alter-ego refusal, chosen rather than random discard, foreign-card rejection,
empty-hand resolution, and persistence after the obligation leaves play.
A prepared pre-turn reveal then progresses real Game turns and proves the
restriction survives the other player's turn and ends with Scott's next turn.
The existing generic duration fixture now uses Thor in its second seat instead
of a duplicate Ant-Man identity, with its assertions retained.

Source verification passes all 86 focused Content/data checks, including the
actual card and affected duration/reprint classes, after a forced non-incremental
build. Both-transport catalog and runtime admission checks pass all 12 cases;
the incomplete pack remains excluded. The valid runtime JSON mutation
`EndOfNextPlayerTurn` to `EndOfRound` fails the real-turn expiry case with five
controls passing. Changing `yourAlterEgo` to `you` fails both hero-form removal
refusal cases with four controls passing. Each original byte was restored.
Logs are `cassie-actual-{focused,duration-mutant,alter-ego-mutant,restored-build,
restored-tests,catalog}.log` under `C:/Users/mggar/Source/evidence/`.

Independent review remains pending. No complete public journey, native
interaction or replay publication claim is made here.
