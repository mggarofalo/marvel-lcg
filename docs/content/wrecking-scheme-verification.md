# Signature scheme verification

MARVEL-625 includes all four signature schemes and both Breakout faces. The
printed interpretations are exercised through the canonical ability book by
`WreckingSchemeFixture`. Setup recipes own the four paired schemes, their starting threat,
the selected stages and Wrecker's initial counter. The rules corpus confirms that
these schemes remain at zero threat while their paired villain is in play and
leave only on its final defeat, including Extreme-mode promotion.

The focused controls cover thresholds below, at and above ten for one through four
players; friendly heroes, alter-egos and allies; tied highest-cost discards; players
without eligible cards; independent cleanup after Tough prevention; and cleanup
only after all player choices finish. Global encounter effects receive no player
credit, while an unbound player-card effect still raises (MARVEL-668). Reinstating
the unconditional resolver lookup kills five controls. The full Content suite
passes 3,391 cases at this checkpoint.

`WhenVillainPhaseStepOneCompleted` is an engine-chosen completion event, observed
at the actual start of villain step one after preceding phase-end work. Its
boundary remains queued when the main threat assignment is prevented or replaced.
Only an in-play observer allocates that extra frame, preserving ordinary replay
identity. `WhenVillainPhaseStepOneEnds` retains the established positive-placement
meaning used by Assault on NORAD; its authored definition remains unchanged.
Six boundary controls include normal, prevented, replaced and terminal placement,
observer entry after planning, and unobserved scheduling. Suppressing the boundary
kills four controls (MARVEL-666).

This is not full-pack acceptance. MARVEL-669 requires one shared response window
for a multi-scheme threat instruction, including first-player ordering when two
signature schemes reach their thresholds together. That obligation follows
`rr:response.3`, `rr:triggering-condition.2`, and `rr:simultaneous-resolution`.
Simultaneous-window integration, full-pack admission, saved full games and
native acceptance remain required.

Integration on 2026-10-25 combines the observed turn/phase boundary foundation with the canonical scheme rows. Draft row injection is removed. Duplicate `WreckingSignatureSchemeTests` and `WreckingSchemeChoiceTests` are superseded by `WreckingRemainingSchemeTests` and `WreckingRemainingSchemeChoiceTests`, retaining their assertions and corrected first-player/sole-target semantics. The focused canonical foundation run passes 75 cases (`%TEMP%/wrecking-foundation-focused.log`).
