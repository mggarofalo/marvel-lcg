# Behavioural specs

The authority-to-obligation derivation, legal-scene rules, and executable
completion criteria are defined by
[`docs/behavioral-specification.md`](../docs/behavioral-specification.md).

The spec tree has three roles:

| Path | Status |
|---|---|
| `behavior/core/` | admitted Core Set transcripts executed against the C# engine |
| `cards/`, `rules/` | inherited draft prose; parseable, but not executable evidence |
| `self-test/quarantine.feature` | deliberately false transcript proving that the runner rejects a wrong observation |

Prior existence does not admit a draft. An executable transcript must be
derived from the repository's authorities, start from a canonical legal scene,
and name a catalog obligation. The Rules Reference under
`datasets/rules-reference/entries/` decides rules behaviour; printed card text
comes from `datasets/cards/`.

`GherkinFormatTests` parses the complete tree and checks its tags and escaping.
`Marvel.Behavior.Run` binds and executes only the admitted corpus:

```bash
dotnet run --project tools/Marvel.Behavior.Run -- check
```

## Quoted arguments

Card and option names are delimited with double quotes in step text. A literal
double quote inside one of those names is written as `\"`; for example,
`"I'm Tough"` is written as `"\"I'm Tough\""`. A literal backslash is written
as `\\`.

This escaping is a repository format choice. Gherkin itself does not assign
meaning to quotes or backslashes inside a step. A binding decodes `\"` and
`\\` after Gherkin parses the text.
