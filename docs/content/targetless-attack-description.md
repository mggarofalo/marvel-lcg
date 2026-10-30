# Describing an attack without one fixed target

MARVEL-875's live-choice source `e1f5f267`, with test-layout closure `bae99bb0`,
records a real attack-end boundary with the hero as source and target `-1` after
damaging a minion and the villain. Its causal comparison passes against the
uninterrupted attack. That boundary does not establish that no enemy was attacked.

The View correction on base `ee1f60de` describes the supplied source and whether
the attack begins, resumes or resolves. It retains the same authorized anchors
and activity subjects. Boundaries naming a target retain their existing text.
No card text is interpreted, target list invented, or protocol contract changed.

`dotnet test tests/Marvel.View.Tests/Marvel.View.Tests.csproj -c Release -m:1
--filter 'FullyQualifiedName~EventActivityProjectionTests|FullyQualifiedName~EventPresentation|FullyQualifiedName~AttackProjection'`
passes 66 cases, including six exact-description checks for restricted and
cooperative policies across start, resume and completion. The log is
`C:/Users/mggar/Source/evidence/roars-targetless-view-focused.log`.
These are projection and wording checks; no rendered/native journey is claimed.
Independent review accompanies the live-choice source before integration of that
capability. The full pack and replay publication gate remain open.
