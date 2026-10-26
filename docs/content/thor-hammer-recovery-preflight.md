# Hammer Throw recovery preflight packet

MARVEL-758 engine packet. Public Thor admission remains closed.

The labelled attack projection now retains an opaque recovery boundary instead of predicting the encounter draw or the minion's survival. The accepted operation has no cost or ability predicate: a minion's own mandatory interrupt discards one encounter card, tests its physical discarded receipt for treachery, and heals that same minion's damage. These bounds constrain the possible mutations to the encounter piles and the recovering subject. Arbitrary interrupt bodies may move the returned card, change ownership or health, or require choices; they remain unsupported.

After that boundary, one immediate return of a uniquely selected owned in-play upgrade is supported. Both the recovering minion and returned upgrade must have no hosted cards. All live authored constants must consist only of attack or trait grants (including their conditional/sequence structure), and no registered health effect may exist. Thus the return cannot change health or defeat eligibility through another live constant. Other later board reads remain behind the existing opaque defeat guard. This is semantic operation/topology analysis: no card title or face id authorizes the exception. The DSL's title selector still resolves the exact returned physical upgrade.

These conservative bounds explicitly refuse ordinary legal Thor boards containing Thor's Helmet, Endurance or any other health constant; unanalysed constants and attachments also refuse. This packet does not establish full-pack readiness. Broadening the dependency analysis is remaining engine work, not a content-authoring workaround.

Authority is printed 06005/06028 in datasets/cards/cards.json, the mandatory Loki erratum in datasets/marvelcdb-faq/faq.json, rr:damage.step.6/9 and rulings 8fe224a8f1e012b2/c55100b5139ab3a5. The actual-row synthetic board covers both treachery survival and actual defeat, paid three resources and exhausted Mjolnir before target selection, eight attack damage with excess four to the villain, returned Mjolnir and removed its attack modifier before the optional defeat response, and Hall of Heroes credit only for an actual defeat. Negative controls retain digest/RNG and already committed costs on unsupported target admission, and reject later board-dependent damage.

Composition evidence uses Rules source 8265a038 (local cherry-pick 80814897), which is independently under review for shared damage-stage ordering and is not accepted by this packet. No public setup/save/replay or native interaction claim is made. The admitted recovery shape itself contains no new player decision. The original OverkillRecoveryCardTests refusal test on that dependency must be reconciled during integration because successful bounded admission replaces its expected exception.

External evidence under C:/Users/mggar/Source/evidence:

- hammer-preflight-focused.log: focused regression run before registered-health negative.
- hammer-preflight-mutant.log: compiled mutant bypassing constant dependency validation; the health-dependency test fails because no exception is raised.
- hammer-preflight-restored-build.log: restored nonincremental build.
- hammer-preflight-restored.log: restored focused regression run, including registered-health negative.
- hammer-preflight-architecture.log and hammer-preflight-commit.log: architecture and normal commit-hook results.