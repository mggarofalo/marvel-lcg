# Independent Hammer Throw recovery review

Reviewed 1d7d41e1 + 2a0a4efd + 873a9be6, combined with current actual Loki and RR 1.8 source at 33da5af3. Isolated merge revision 02a71968.

No blocking production finding in this bounded review. Inspected the exact self/attached full-heal patterns, relevant constant-health tracing and physical return selection. Both Biomechanical orders distinguish cancellation of remaining would-be-defeated work: attachment-first leaves the encounter top untouched; Loki-first treachery leaves Biomechanical attached; Loki-first non-treachery permits its full heal/discard. Actual Zola attachments distinguish recovery retention from defeat departure.

Added four adversarial cases: repeated Actions/Describe queries preserve card digest, RNG state/consumption and pending operation payloads, then match an unprobed execution; an intentionally synthetic duplicated Mjolnir tableau refuses an ambiguous return atomically. These duplicate copies do not establish a legal card-combination refusal.

The combined 38 focused Hammer Throw/Loki/RR 1.8 cases pass. A compiled mutant removing cardinality/ownership checking is rejected by both ambiguous-return cases. Restored sources were rebuilt nonincrementally and the same 38 cases pass. Evidence logs are outside the repository under C:/Users/mggar/Source/evidence/hammer-review-*.log. Normal merge hooks passed architecture-policy (66 tests), complexity and file length; review commit hooks remain enabled.

This review does not certify public Thor admission, native interactions, save/replay, other optional/conditional recovery bodies or dynamic health-recipient shapes. Unsupported target selection is checked at the pending target commitment after event costs; these tests do not claim payment rollback. No Rules source or ability-book change was authored by this reviewer.