# Voluntary identity form timing — MARVEL-863

Candidate based on `9f8f79507717aa67b0ad850a2f2808b98afca32b`. The voluntary
turn option schedules the same exact-identity, incarnation-bound operation used
by ability-driven changes. RR 1.8 `rr:interrupt.3` places interrupts before
the flip and `rr:response` places responses after it. The explicit three-face
destination and existing two-face offer identity remain unchanged.

`rr:form-change-form.1` permits one voluntary change per round. The engine
records that permission as used when the flip occurs; `rr:cancel.5.1` says a
canceled effect is not considered to have occurred. Cancellation or live
invalidation settles the existing completion receipt at zero and suppresses
its response window. The physical card and its incarnation are revalidated at
application. `CardFormChanged` is emitted at the actual voluntary flip.

Normal `Game.Resolve` fixtures cover old/new observations, actual Taskmaster
hero-to-hero and alter-ego controls, cancellation and nested changes. A synthetic
suspended window checks revalidation of the round permission. Existing three-face
fixtures cover invalid, unknown, current and stale explicit targets and the round
limit. The server rejects decisions against an earlier session revision; changing
the stable two-face offer identity to accommodate direct external world mutation
would not represent the supported server path.

On this candidate, 48 focused form tests, seven lifecycle tests and 67 Core
transcript tests passed. Removing the voluntary round-permission guard compiled
and failed the synthetic test on the incorrect flip. Restoring the guard, forcing
a non-incremental build and rerunning all 48 focused tests passed. Logs are in
`C:/Users/mggar/Source/evidence/voluntary-*.log`. Independent review and CI remain
required.

This changes gameplay timing and potentially accepted decision sequences and
replay event order. Publication requires a new post-Ronan replay compatibility
boundary; this packet does not change the replay version or claim compatibility
with earlier ledgers. The legacy direct `Forms.ChangeAndSchedule` helper and
additional-form changes are outside this voluntary-path packet.
