# marvel-lcg

A rules engine for Marvel Champions: The Card Game, written in C#.

The supported runtime product is the Core Set: its 5 heroes, 3 scenarios in
Standard and Expert, 5 modular encounter sets, and all 209 card faces. Broader
card and rules datasets support research, but later products are not executable
content. [Product and repository scope](docs/scope.md) defines that boundary.

The engine's job is to be **right about the rules**. Every behaviour it
implements is held against the published Rules Reference, vendored in
[`datasets/rules-reference/`](datasets/rules-reference/), and tests cite the
clause they come from by id — `rr:attack-enemy-activation.step.3`. A rule the
engine does not implement raises rather than guesses, because a board that is
plausible and wrong is worse than a board that stops.

## Layout

| Path | What |
|---|---|
| [`src/`](src/) | The engine. `Marvel.Core`, `Marvel.Rules`, `Marvel.Cards`, `Marvel.Content` |
| [`tests/`](tests/) | The behavioral and contract test suites |
| [`datasets/`](datasets/) | The rules, the cards, and what a scenario is dealt from |
| [`specs/`](specs/) | Gherkin scenarios written from printed card text — drafts, see [specs/README.md](specs/README.md) |
| [`docs/`](docs/) | Design documents and wire-format specifications |

## Running it

```
dotnet build Marvel.slnx -c Release
dotnet test Marvel.slnx -c Release
```

Contributors should start with [AGENTS.md](AGENTS.md).

## Documents

| Document | Description |
|---|---|
| [The Card DSL](docs/card-dsl.md) | How a card's printed text becomes data the engine runs |
| [Product and Repository Scope](docs/scope.md) | What is executable and what remains research input |
| [The Card Dataset](docs/card-dataset.md) | The joined card data behind the card port |
| [The Setup Dataset](docs/setup-dataset.md) | What a scenario is dealt from |
| [Rules Provenance](docs/rules-provenance.md) | Which published source decides what, and what happens when one moves |
| [Rules Citations](docs/rules-citations.md) | How a test cites a rule, and what an uncited test honestly is |
| [Timing](docs/timing.md) | Ability timing, interrupt and response windows, continuous effects |
| [Places](docs/places.md) | Play areas, game areas, and anything resolving by where a card is |
| [Event Stream](docs/event-stream.md) | What the engine tells a client changed |
| [Affordances](docs/affordances.md) | What the engine tells a client the player can do |
| [State Digest v2](docs/state-digest-v2.md) | The canonical serialisation of a board |
| [RNG Contract](docs/rng-contract.md) | The random number generator specification |
| [Presentation Layer](docs/presentation-layer.md) | The plan for a client |
| [Release Policy](docs/release-policy.md) | Artifact versions, compatibility, signing and upgrade rules |
| [Plane](docs/plane.md) | How work is tracked |

## Origin

This repository began as a fork of
[irefrixs/marvel-lcg](https://irefrixs.itch.io/marvel-lcg), a Python
implementation of the same game. None of that code remains; the rulebook, not
that engine, decides what this one does.
