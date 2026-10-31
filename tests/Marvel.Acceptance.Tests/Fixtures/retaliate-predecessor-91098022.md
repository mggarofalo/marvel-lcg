# Original retaliation replay evidence

`retaliate-predecessor-91098022.jsonl.gz` contains the exact simulator output
from untouched predecessor commit
`91098022d37ce739dd2880e8964b40ee3675eeb3`, generated on 2026-10-20.
No engine source, board state, decisions or journal records were patched.
The original runtime independently replayed all 30 decisions without divergence.

Original command, with the output path shortened for portability:

```powershell
dotnet src/Marvel.Sim/bin/Release/net8.0/Marvel.Sim.dll run `
  --scenario rhino --difficulty standard --hero black_panther `
  --seed 700 --policy-seed 13 --decision-limit 100 `
  --output retaliate-predecessor-91098022.jsonl --repo-root $PWD
```

Original uncompressed bytes: 176,781.
SHA-256:
`f9107037687344f2cfde00005e3acc2a5494894b1f905c0ef8e94aae1b0e7be1`.
Gzip has a zero timestamp; compressed SHA-256:
`10d6a3ba12d4df6fcf612a9440fbaac8953ae14c9881c734b5732fc481ec0b47`.

The first changed decision is step 9, a legal basic Defense with Black Panther.
The original zero-damage enemy attack omits retaliation. MARVEL-635 adds the
required retaliate damage after attack completion: 18 old events become 19,
and the digest changes. Old digest:
`d50cd80eedae7b2308d2c84fc6011f6a07c16b72028c90828c0d52375db2d38b`.
Checkpoint-635 digest:
`77c777ea8a31016391770494a06f430472b818d2cf831a7cbb5791bd030e5252`.

This fixture is a frozen game journal kept as regression evidence: strict replay
rejects the original trace at the changed retaliation outcome rather than
rewriting it. It is not a save, and no save from that runtime is loadable.
