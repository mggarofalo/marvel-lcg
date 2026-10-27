# Socket composition fixture initialization

The combined integration run at `ed03a479` fails
`ExplicitSocketConfigurationDoesNotReadLocalContent` after 34 seconds with an
aborted socket write. The exact unchanged whole class passes all 17 cases in
isolation (18 seconds). The fixture constructs and loads its dataset-backed
server after accepting the client connection, inside the client's bounded
exchange interval. Under the concurrent full lane this initialization can consume
the exchange deadline.

The fixture now constructs the same dataset-backed server synchronously before
returning its serving task and before the client starts its exchange. The client
still receives a nonexistent local content directory, the server still uses the
actual dataset, and every existing assertion and production timeout is unchanged.
This is test initialization ordering, not a transport exception waiver.

Evidence: `Source/evidence/strange-combined-integration.log`,
`strange-socket-fixture-baseline.log` and `strange-socket-fixture-restored.log`.
The combined integration rerun and independent review remain required.
