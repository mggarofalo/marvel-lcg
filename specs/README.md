# Behavioural specs

112 Gherkin `.feature` files, one per card or rule, written from printed card
text. Each opens with the card as printed and reasons about the branches that
text implies, so the file is readable as an argument and not only as a fixture.

## Status: every one of these is a draft

They were written while the Python engine was the reference, and validated
against it. That validation is retired. `datasets/rules-reference/entries/*.md`
decides behaviour now, and a spec that disagrees with the rules is wrong no
matter what it once passed against.

Removed with the Python engine, and recoverable from git history if ever
wanted:

| File | What it recorded |
|---|---|
| `trusted.json` | scenarios that passed against the Python engine, pinned by source hash |
| `quarantine.json` | scenarios that did not pass, each a spec bug or an engine bug |
| `unreachable.json` | scenarios the harness could not reach |
| `history.jsonl` | per-run verdict history |
| `steps.catalogue.json` | the step vocabulary `tools/spec/` bound |

Nothing replaced them, deliberately. A trust marker whose writer no longer
exists is a claim nobody can recheck, and the claim it made was about the wrong
authority. These are drafts until a C# runner re-validates them against the
rules.

## What holds them today

`tests/Marvel.Core.Tests/Specs/GherkinFormatTests.cs` parses all 112 with the
standard Gherkin grammar — the parser Reqnroll is built on — and asserts that
`@card:` and `@rr:` tags survive. That is a check on the *format*, so the files
stay loadable by whatever runs them later. Whether the steps can be *bound* is a
separate question and a later one; see `docs/presentation-layer.md`.
