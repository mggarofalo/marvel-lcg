# Historical protocol comparison fixture

Based on `ed03a479`, the protocol-35 comparison reader lacks the empty
`world.invocation_decks` field required by protocol 42. The current decoder
rejects its absent value as invalid Invocation references before the actual
predecessor-process save test can compare the authorized responses.

`HistoricalProtocol35Response` is a test-only projection of the archived
Core/RoRS responses. It adds an empty typed Invocation list to the parsed
comparison copy, alongside the existing empty encounter-group and offer-context
normalizations. These historical fixtures have no Invocation piles. Any existing
`invocation_decks` member, including null or an empty list, is rejected because
protocol 35 has no such field. The reader still requires exactly version 35 and
passes the result through the strict production decoder.

The archive `tests/Marvel.Server.Tests/Fixtures/core-session-91098022.zip`
retains SHA-256
`63BD6E38C7668F6789BB48300165026D34CA55432B59ADF08FF2B19793E87ABC`.
The original predecessor-process test is unchanged: its independently produced
old-runtime response remains the control for restoration and continuation.
No runtime contract, version admission, save migration or archived byte changes
are part of this repair.

## Verification

Evidence is under `C:/Users/mggar/Source/evidence/strange-protocol-fixture-`:

- `tests-restored.log`: all 38 cases pass across the complete historical reader,
  actual predecessor-process, Invocation transport and independent area-contract
  review classes.
- `overwrite-mutant.log`: a compiled adapter mutation that overwrites unexpected
  Invocation data fails all four rejection controls.
- `omission-mutant.log`: a compiled adapter mutation that omits the empty list
  fails all four archived-response controls.
- `final.log`: the same 38 cases pass after restoring the exact adapter source.

The new controls preserve the caller's input bytes, verify the original archive
hash, reject protocol 42 in this historical reader, reject unexpected Invocation
values and unknown world members, and confirm that the current decoder still
rejects an omitted Invocation list. Existing transport controls retain null,
ownership, aliasing and dangling-reference rejection. An initial test run caught
a transcription error in the new hash assertion; the assertion now uses the
unchanged original pin above. That failure is test-authoring evidence, not a
product mutation.

This packet establishes the narrow comparison-fixture repair on Windows. It
does not claim a complete Server suite, native interaction or second-platform
run. Normal commit hooks and independent review remain required for integration.
