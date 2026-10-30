# Independent fixture review

Accept the narrow socket-initialization fixture changes from `7ab1475d4420103a61605a3a6debd3a346579fb1`
and `bdcd5491`, reviewed on `ed03a479` in a separate worktree with both changes
applied. No runtime change or additional test control is needed for this scope.

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
