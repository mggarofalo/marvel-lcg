# Independent Thor composition review

Reviewed candidate: `16870d984c0e8a40972eef5d981fdbdda4c4bc79`.
Verdict: no blocking finding in actual 06005 Hammer Throw, 06011 Hercules and
06015 Mean Swing. The complete book contains 598 faces, including 22 Thor faces.
Valkyrie, public Thor admission and starter journeys are outside this review.

Authority review used generated `datasets/cards/cards.json`, the three authored
rows, vendored FAQ/rulings searches, and RR1.8 `rr:and`, `rr:cost.7`,
`rr:initiating-abilities.step.5`, `rr:ally-limit` and `rr:unique-icon.4.1`.
Hammer Throw's damage and return joined by “and” precede the later optional
defeat response; the candidate's Chase Them Down probe observes the returned
physical Mjolnir during that response and its nested choice. Its paid/stunned
cases distinguish canceled attack effects from retained paid costs. The real
Mjolnir's departure ends its ATK and Aerial effects.

`ThorCompositionIndependentTests` adds five cases using actual rows and printed
cards on synthetic supported boards. Hammer Throw is initially offered with a
controlled Mjolnir, then loses that offer when the same physical hammer changes
controller; replaying the stale action rejects without changing digest, RNG or
pending work. Hercules reprices after its controller's engaged minion transfers
to another seat, retains a one-use discount after underpayment, and successfully
pays the new price. At a zero price from stacked reductions it still requires
the ally-limit discard and prohibits a duplicate unique Hercules. Mean Swing
selects Mjolnir among two actual attached Weapons, leaves Jarnbjorn ready, pays
no resources, and expires its bonus; declining keeps both Weapons ready and the
event in hand. Its prompt identifies the hero and target and current bonus.

Focused verification selected the independent class, the candidate's three card
classes, AbilityData and RuleCitationTests: 54 tests passed. The restored source
was rebuilt with `dotnet build tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj
-c Release --no-incremental`, then the same selection passed with `--no-build`.
Logs are outside the repository at `C:/Users/mggar/Source/`:
`thor-composition-independent-restored.log`, `thor-composition-review-rebuild.log`
and `thor-composition-review-mutant-restored.log`.

A compiling temporary mutation removed the controller restriction from
`NamedAbilityCost.Target`. The foreign-hammer test failed because Hammer Throw
was incorrectly offered. `thor-composition-review-mutant-foreign.log` preserves
that distinguishing failure. The production source was restored byte-for-byte
before the nonincremental rebuild; no engine or ability-book change is retained.
The first experimental test incorrectly expected direct `CardPlay.Play` to
execute an event action; it was corrected to use the actual offered action.
An attempted `dotnet test -t:Rebuild` command was rejected by argument parsing;
the separate build and test commands above supply the successful restoration
evidence. No broad product-readiness or native acceptance claim follows.

Tools were restored and Husky installed normally. The review commit uses normal
pre-commit hooks; its output is `thor-composition-review-commit.log` beside the
other external logs.
