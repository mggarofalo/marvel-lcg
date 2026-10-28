# Ant-Man B1 reprints — MARVEL-569

Baseline `9fc46f541fb1466880766131f47e3872890c5c8a`. This packet authors only First Aid `12019`, Energy `12021`, Genius `12022`, and Strength `12023`; it does not admit Ant-Man setup or the remaining faces.

The generated card catalog gives the same printed action, cost, and resource for `12019` as `01086`, and the same resource icons and max-one-per-deck text for `12021`–`12023` as `01088`–`01090`. The authored rows reproduce those existing interpretations. Focused tests exercise actual First Aid targets and missing damage, resource generation and payment, and title-based mixed-reprint deck limits; the inventory retains unauthored identity and Swarm Tactics controls.

On the B1 candidate, the Release focused, ability-data and inventory filter passed 111 tests after a non-incremental rebuild. A compiled test run with the 12019 heal amount changed from 2 to 1 failed on the damaged villain (expected 1 remaining damage, observed 2); the restored row passed the same focused filter. Independent review is required after commit. The complete pack, setup, and gameplay remain outside this packet.
