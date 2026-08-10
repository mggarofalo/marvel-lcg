The open-source of Marvel LCG digital version on [ITCH](https://irefrixs.itch.io/marvel-lcg)

## Playing the game

| Guide                                                          | Description                     |
| -------------------------------------------------------------- | ------------------------------- |
| [Install Guide](docs/install_guide.md)                         | How to install and run the game |
| [How to Play](https://itch.io/t/3763917/how-to-play-this-game) | Game rules and controls         |
| [Debug Guide](docs/debug_guide.md)                             | How to debug the game           |
| [Editor Guide](docs/editor_guide.md)                           | How to use the card editor      |

## Security Warning

This game runs Python card scripts, which is not safe.  
Do not install or run any third-party card scripts unless you trust them.

这个游戏会运行用 Python 编写的卡牌脚本，这不安全。  
除非你完全信任，否则不要安装或运行任何第三方的卡牌脚本。

## Snapshot

![](/docs/assets/image-1.jpg)
![](/docs/assets/image-2.jpg)

## Development

The game as it exists today is the Python engine in [`py_src/`](py_src/); it is being
migrated to C# in [`src/`](src/). Contributors should start with
[AGENTS.md](AGENTS.md) for how to run and work in this repo, and
[Migration to C#](docs/migration.md) for why the migration is happening and what has
already been decided.

| Document                                                | Description                                                                |
| ------------------------------------------------------- | -------------------------------------------------------------------------- |
| [Migration to C#](docs/migration.md)                    | Why the migration is happening and what has been decided                   |
| [Engine Architecture](docs/engine_architecture.md)      | Engine internals for developers                                            |
| [Card Scripting Guide](docs/card_scripting_guide.md)    | How to write card ability scripts                                          |
| [The Card Dataset](docs/card-dataset.md)                | The joined card data behind spec authoring and the card port               |
| [Behavioral Spec Harness](docs/spec-harness.md)         | How a card's printed text becomes an executable claim about the engine     |
| [Card Coverage](docs/card-coverage.md)                  | The metric that decides whether the replay corpus is worth anything        |
| [Runtime Invariants](docs/invariants.md)                | What must be true of the world at every decision the engine takes          |
| [Determinism Audit](docs/determinism-audit.md)          | Whether the Python engine is deterministic, which the corpus depends on    |
| [RNG Contract](docs/rng-contract.md)                    | The cross-engine random number generator specification                     |
| [State Digest v2](docs/state-digest-v2.md)              | The current state-digest (CRC) cross-engine contract                       |
| [State Digest v1](docs/state-digest-contract.md)        | **Superseded.** The v1 digest, kept for scenes saved before `0.5.9.205`    |
| [Plane](docs/plane.md)                                  | How work is tracked in the Plane workspace                                 |
