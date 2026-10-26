# Timed printed text blanking prerequisite (MARVEL-734)

Private candidate based on 0d2a33ab implements a standalone self-blank Action
for a live nonpermanent minion until phase end. No actual 05028 row or public
admission is included. Printed text authority is datasets/cards/cards.json;
Rules Reference blank/traits/lasting effects determine suppression and expiry.
Traits, printed basic stats and icons outside the text box remain. The opcode
is an engine spelling; compound shapes and nonminion/permanent targets are
rejected before cost. The existing engine has no alternate-win minion hook or
DSL win/loss opcode. Any future alternate-win text needs explicit preservation
under rr:winning-the-game.1.1 before admitting it to this capability.

Active text filtering covers action/resource offers, derived and registered
constants, lifecycle discovery, damage protection, keywords and persistent
ability descriptions. Already-triggered continuations retain their compiled
body. Lasting effects already created remain. Markers bind the in-play
incarnation and expire through the phase scheduler.

Evidence: 14 focused cases pass after forced rebuild; final full Content 5,088 pass
in external blanking-final-content.log. Compiled registered-constant and
lifecycle-discovery mutants fail 1 and 2 intended assertions; forced restoration
passes 14. Earlier overlapping build failed with locked DLLs; that failed run
is not proof. Tests include phase expiry, offphase restoration, numerical
keywords, pending continuation, permanent cost rejection and replay of identical
synthetic worlds. This is not hosted persistence evidence.

Independent review remains required, especially projected-vs-live constant
queries, pending ordinal lookups and timing observers that may retain extra
scheduler boundaries despite suppressing actual waiting abilities. Current
native interaction uses existing encounter-action patterns; actual face tests
and any necessary native proof follow engine approval.
