# Beat Cop retained public quantity repair

Candidate extends e50a87fe2def07ca87a85b73fc491287dba1cc8b. A legally empty
player deck immediately recycles discarded Beat Cop. Its pending damage choice
retains the known public threat quantity even though the source is now hidden.

The engine captures whether the exact source was face up in play before paying
costs, and carries that fact through continuation memory. Current in-play reads
still require face-up visibility. Departed-source disclosure requires the exact
incarnation, captured public provenance and the admitted exhaust/discard cost
shape. Synthetic hidden-origin probes cannot manufacture that provenance.

Evidence in ../evidence relative to the worktree:

- hulk-meaning-provenance-content.log: 13 focused content cases passed.
- hulk-meaning-provenance-session.log: 84 session cases passed.
- hulk-meaning-provenance-valid-mutant.log: compiled inverted-public-provenance
  mutant failed all four paid-choice cases (both visibility policies, empty and
  nonempty deck); three other cases passed.
- hulk-meaning-provenance-restored-build.log and restored-godot.log: exact source
  bytes restored, forced rebuild succeeded, 21 Godot semantic cases passed.

Two earlier mutation attempts are excluded: one selected no tests; the other
failed a compiler analyzer before execution. Only the compiled mutation above is
behavioral evidence. Independent closure of the original review remains required.
