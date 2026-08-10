# Install Guide

The game lives in [`py_src/`](../py_src/). **Every command below runs from `py_src/`, not
the repo root.** Python paths in the engine are relative to the working directory —
`launch.json` points at `./data/`, `./replays/` and `./assets/` — so starting from the
repo root leaves the engine unable to find its data.

## 1. Install Python 3.13

The engine is pinned to 3.13 by [`py_src/.python-version`](../py_src/.python-version).
Get it from https://www.python.org/downloads/.

## 2. Install the dependencies

Dependencies are managed with [uv](https://docs.astral.sh/uv/).
`requirements.lock` is the pinned resolution — install from it, not from
`requirements.txt`, so you get the versions the project is tested against.

```cmd
cd py_src
uv venv --python 3.13
uv pip install -r requirements.lock
```

<details>
<summary>Without uv</summary>

```cmd
cd py_src
py -3.13 -m venv .venv
.venv\Scripts\pip install -r requirements.lock
```

</details>

## 3. Compile the web client

The client is TypeScript, and the compiled JavaScript is gitignored — a fresh clone has
none, so this step is required for the browser UI. (The HTTP API works without it.)

1. Install Node.js: https://nodejs.org/en/download
2. Install the compiler: `npm install -g typescript`
3. Run `watch.bat` in `py_src/public/js/` — it runs `tsc --watch` against
   [`py_src/public/js/tsconfig.json`](../py_src/public/js/tsconfig.json) and keeps
   recompiling as you edit.

## 4. Download the assets (optional)

The engine runs without an `assets/` folder. Card images are fetched from the
`image_servers` listed in `py_src/launch.json`, and anything missing is drawn as a
placeholder by `engine/lib/image_creator.py`.

For the real art, download the game from
[itch.io](https://irefrixs.itch.io/marvel-lcg) and put its `assets` folder in `py_src/`.

## 5. Start the game

```cmd
cd py_src
.venv\Scripts\python.exe main.py
```

Then open http://127.0.0.1:2345/main.

To check it came up without a browser:

```cmd
curl -s -o /dev/null -w "%{http_code}" http://127.0.0.1:2345/main
```

`200` means the server is serving. Most other API routes additionally require an
`app_version` cookie — see the notes in [AGENTS.md](../AGENTS.md) if you are calling the
API directly.
