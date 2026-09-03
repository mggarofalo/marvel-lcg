# Proving presentation ownership

Four projects declare one presentation role and reference `Marvel.Core`, which
that role must never compile against. The Transitive probe uses an allowed
reference but leaves transitive project references enabled. The script accepts
only the corresponding `MARVELPRESENTATION` or `MARVELPRESENTATIONCONFIG` error.

The real presentation projects are the positive probes. They use the same
path-normalized allowlists whenever the solution builds. This combination
proves that every allowed graph builds and every forbidden edge fails.
