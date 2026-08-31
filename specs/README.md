# Behavioural specs

The authority-to-obligation derivation, legal-scene rules and executable
completion criteria are defined by
[`docs/behavioral-specification.md`](../docs/behavioral-specification.md).
Feature files under `cards/` and `rules/` are candidate prose until they are
independently derived through that contract; prior existence gives them no
coverage or trust. Admitted transcripts live under `behavior/core/` and pass
the executable C# runner.

The inherited Gherkin files, one per card or rule, were written from printed card
text. Each opens with the card as printed and reasons about the branches that
text implies, so the file is readable as an argument and not only as a fixture.

## Status

The files under `cards/` and `rules/` were written while the Python engine was the reference, and validated
against it. That validation is retired. `datasets/rules-reference/entries/*.md`
decides behaviour now, and a spec that disagrees with the rules is wrong no
matter what it once passed against.

`behavior/core/` is a separate passing corpus. Each scenario names one catalog
obligation, starts from the canonical legal scene constructor, and runs against
the C# engine. `self-test/quarantine.feature` remains deliberately false and is
executed separately to prove a wrong observation fails.

Removed with the Python engine, and recoverable from git history if ever
wanted:

| File | What it recorded |
|---|---|
| `trusted.json` | scenarios that passed against the Python engine, pinned by source hash |
| `quarantine.json` | scenarios that did not pass, each a spec bug or an engine bug |
| `unreachable.json` | scenarios the harness could not reach |
| `history.jsonl` | per-run verdict history |
| `steps.catalogue.json` | the step vocabulary `tools/spec/` bound |

Nothing replaced these trust-marker files. Admission now follows from a passing
authority-derived transcript rather than a mutable list of historical verdicts.

## What holds them today

`tests/Marvel.Core.Tests/Specs/GherkinFormatTests.cs` parses the complete tree with the
standard Gherkin grammar — the parser Reqnroll is built on — and asserts that
`@card:` and `@rr:` tags survive. That is a check on the *format*, so the files
stay loadable. `Marvel.Behavior.Run` separately binds and executes only admitted
transcripts:

```bash
dotnet run --project tools/Marvel.Behavior.Run -- check
```

## Quoted arguments

Card and option names are delimited with double quotes in step text. A literal
double quote inside one of those names is written as `\"`; for example, the
printed names `"I'm Tough"` and `The "Immortal" Klaw` are written as
`"\"I'm Tough\""` and `"The \"Immortal\" Klaw"`. A literal backslash is
written as `\\`.

This escaping is a format chosen by this repository; the Gherkin grammar does
not assign meaning to quotes or backslashes inside a step. A future step binding
must decode `\"` and `\\` after Gherkin parses the step text. The format test
holds the escape markers to the exact spelling that parser preserves.
