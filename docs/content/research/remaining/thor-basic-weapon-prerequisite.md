# MARVEL-747: basic-use and Weapon cost prerequisite

Candidate derives from `f1935a21`; no ability-book, setup, admission or release
version changes. Printed authority: generated `datasets/cards/cards.json`, 06015:
Hero Interrupt when your hero makes a basic attack; exhaust a Weapon upgrade on
your hero as the arrow cost, then +3 ATK for this attack. The vendored FAQ search
found no 06015 entry. RR1.8 `rr:attack-player-ability-type.1` distinguishes the
basic power from labeled attacks (`.2`); `rr:cost.7`, `rr:exhausted.2`, and
`rr:initiating-abilities.step.5` govern payment/control and rejected costs.

The engine chooses the condition and DSL spelling. The bonus belongs to the
existing occurrence, avoiding a global stat modifier or global attack-end
expiration. BasicUse currently identifies attack only; E5b still needs thwart
and the incarnation-bound selected ally's matching modified power.

Focused synthetic `BasicUseWeaponTests` exercise both selected physical copies,
this-attack-only amount, attached/ready/current-trait/control filtering, invalid,
foreign and duplicate choices with digest/RNG/pending-work preservation, retry,
and negative basic-thwart, ally-attack and scheduled card-attack payloads.
The card-attack negative is a scheduled payload probe, not full event-play proof.
The actual ability row and full 06015 product integration remain for the book owner.

Mutation evidence outside the repository:
`C:/Users/mggar/AppData/Local/Temp/thor-e5-mutant.log` removes only the host filter;
it compiles and the wrong-host negative fails. Restored sources are rebuilt with
`--no-incremental` (`thor-e5-restored-build.log`) before `thor-e5-restored-tests.log`.
Normal hooks are installed after `dotnet tool restore` / `dotnet husky install`.

Review boundary: compound attached-exhaustion arrow costs explicitly raise
before execution. Positive adjusted event prices and any supplied resource
payment are refused before commitment, including gratuitous generation at zero.
This prevents resource abilities changing a selected Weapon before exhaustion.
Source incarnation is captured but an attacker leaving and reentering during
its interrupt is not covered here.

Replay impact: the added condition shares the existing attack occurrence and can
change prompt labels where an existing attack-initiation observer is present.
The integration owner must assess replay compatibility under the Thor replay
boundary. RNG and card-digest formats are unchanged. No native/UI readiness is
claimed; semantic descriptions use existing authorized prompt fields.

Attached-upgrade costs additionally refuse any supplied resource payment during
preparation, including gratuitous generation at price zero. Event-price preflight
raises for a positive adjusted price or required icons before payment. A discount
to zero remains within the no-generation boundary. Dedicated negative tests
assert untouched source, generator, Weapon, digest, RNG and pending work.
