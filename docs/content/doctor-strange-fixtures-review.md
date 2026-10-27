# Independent fixture review

Accept the narrow fixture changes from `7ab1475d4420103a61605a3a6debd3a346579fb1`
and `bdcd5491`, reviewed on `ed03a479` in a separate worktree with both changes
applied. No runtime change or additional test control is needed for this scope.

## Historical response comparison

The adapter adds empty Invocation meaning only to its parsed protocol-35
comparison copy. It rejects every preexisting `invocation_decks` member,
including null and an empty array, and requires version 35. The strict production
decoder remains the final reader. Current-protocol omission and unknown-member
controls remain effective; the transport and area-contract selections retain
null, reference, ownership, aliasing and snapshot-isolation checks.

The actual predecessor-process restoration test still compares complete
authorized responses before and after continuation against the independently
recorded old-runtime control. Its implementation and archived bytes are
unchanged. The archive hash is still
`63BD6E38C7668F6789BB48300165026D34CA55432B59ADF08FF2B19793E87ABC`.
The new tests verify that hash and preserve caller input bytes.

The implementation packet's compiled omission and overwrite mutant logs were
reviewed: each fails its four distinguishing controls. This review independently
ran the restored final sources, not those temporary mutations.

## Socket initialization

`ServeOneAsync` now completes dataset loading and constructs the same engine
host/server before returning its serving task. The caller invokes that helper
before connecting and starting the bounded setup exchange. Previously the same
loading ran after accepting the connection, consuming part of the client's
30-second exchange lifetime. Moving fixture initialization outside that lifetime
is appropriate for a test of remote composition without local content.

The nonexistent client content path, actual dataset-backed server, TCP framing,
assertions, exception handling and production timeouts are unchanged. No retry,
exception suppression or relaxed expectation was added. Unreachable-service and
future-schema controls still run in the complete composition class. This repair
does not weaken a transport deadline or conceal failed exchanges.

The original combined-lane aborted-write failure after 34 seconds and the
unchanged class's isolated 17-case pass are consistent with initialization
contention. Those logs do not instrument the exact time spent loading datasets,
so the precise cause of that historical failure remains an inference. The code
ordering independently establishes the fixture defect and the narrow correction.

## Independent verification

Windows Release, with ordinary builds and no skipped selected tests:

- Server filter: complete `HistoricalProtocol35ResponseTests`,
  `PredecessorProcessContentUpgradeTests`, `InvocationDeckTransportTests` and
  `InvocationAreaContractReviewTests`: **38 passed**, 4 seconds reported test
  duration. Log: `C:/Users/mggar/Source/evidence/strange-fixture-independent-protocol.log`.
- Godot managed filter: complete `ClientCompositionTests`: **17 passed**,
  12 seconds reported test duration. Log:
  `C:/Users/mggar/Source/evidence/strange-fixture-independent-socket.log`.

Both commands use `dotnet test <project> -c Release --filter` with each full class
name joined by `|` for the Server selection. Test execution left the worktree
clean before this review document was added.

This verdict covers the two fixture repairs. It does not establish full
integration-lane stability, second-platform success, native interaction, product
acceptance or complete Doctor Strange delivery. The lead's combined integration
rerun remains a separate integration gate.
