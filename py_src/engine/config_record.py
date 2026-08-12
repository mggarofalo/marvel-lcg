"""The fully resolved config, recorded with a corpus and checked against it.

The engine is deterministic **for a given configuration**, not across
configurations: the MARVEL-7 audit measured 158 against 183 forced effects under
different flag combinations, with per-card digests unchanged. So a corpus is
only reproducible if the configuration that produced it is known — and
`ConfigVariables` layers the command line over an arg group over `launch.json`
over the declared default, so no single input file holds the answer. Only the
resolved values do. See MARVEL-34.

Two jobs, and they want different amounts:

- **Recording** takes everything. `Snapshot` writes every registered variable
  with the value it resolved to and the source that decided it, so a manifest
  read years later can say what the run was, not merely what it was asked for.
- **Comparing** takes a subset, because a verifier is a different process doing
  a different job. `Compare` looks only at the variables that change how the
  engine *plays*, and `IsCompared` says which those are.

## What "registered" means

`ConfigVariables.variable_dict` fills up as modules import, because a variable
comes into being when a module-level `ConfigVariables.Int(...)` runs. There is
no declaration list to read instead. So a snapshot is *what this process has
reached*, and it must be taken as late as possible — `BotRunner` takes it when
it writes the manifest, by which point the game has been played and the whole
play path is imported. A variable that no code path touched is absent rather
than wrong, and `Compare` reports that case separately.

## What is compared, and why the rest is not

The cut is by role, not by hand:

- **Paths** (`*_folder`, `*_folders`, `*_file`, `*_files`) describe where this
  machine keeps things. Comparing them means every corpus fails verification on
  any machine but the one that made it.
- **`bot_*`** configures the generator. A verifier never runs one, so every one
  of these would read as drift. What they were is in `resolved`, and the fields
  that decide the game — scenario, heroes, seed, policy, timeout — are
  first-class manifest fields besides.
- **`verify_*`** configures the verifier, which was not running when the corpus
  was made.
- **`INVOCATION`** is the explicit remainder: which device this process drives,
  and what it logs, displays and profiles while doing it. Named one by one
  because there is no suffix to key on.

Everything else is compared, including variables added after this was written.
That direction is deliberate: an unrecognised new flag reads as drift and gets
looked at, where an allowlist would let a new gameplay flag through in silence.
The false positives are visible and the false negatives would not be.
"""

from typing import TypeAlias

from core import *

CATEGORY_NAME = "CONFIG"

# The suffixes `ConfigVariables.Folder/File/Folders/Files` assert on. A path
# variable is identifiable by name because those helpers make it so.
PATH_SUFFIXES = ("_folder", "_folders", "_file", "_files")

# Prefixes owned by one side of the corpus lifecycle. See the module docstring.
ROLE_PREFIXES = ("bot_", "verify_")

# How this process was invoked, rather than how the engine plays. No suffix
# groups these, so they are named.
#
# Nothing that is already excluded by suffix belongs here: `config_files`,
# `translate_file` and `on_startup_load_save_file` all name paths and are
# covered by `PATH_SUFFIXES`. Repeating them would make the list look like it
# was carrying weight it is not.
#
# `statistics` and `pause_test_statistics` are deliberately **absent**. They
# look like bookkeeping that runs beside a game without changing one, and they
# are not: audit finding F5 measured `-no_pause_test_statistics` moving
# `forced_effect` id allocation from 158 to 183, because
# `Engine.statistics.CanRegisterAbility()` decides whether the statistics and
# achievement abilities get registered at all. They are close to the reason this
# whole record exists. See docs/determinism-audit.md.
INVOCATION = frozenset({
    # Which job this process is doing.
    "device", "editor",
    # What it says and draws while doing it.
    "hidden_log_categories", "show_silent_log_categories", "display_sound_name",
    "show_image_text", "save_empty_image", "font", "image_servers",
    "break_when_load_online_image", "check_for_new_version_on_startup",
    "print_my_user_fingerprint",
    # Profiling, which observes the run and does not join it.
    "enable_profile_category", "exclude_profile_functions",
    # Forced from the resolved device by `Engine.Initialize`, which makes it a
    # property of how this process was invoked rather than of what the user
    # asked for: it is off by default and forced on for `-device bot`, so a
    # generator and a verifier can never agree about it. It watches the state
    # and never changes it, and the manifest records what the generating run
    # actually had as a first-class field besides. Comparing it would have made
    # every corpus fail verification in the default configuration -- which the
    # first end-to-end run of this gate duly did.
    "check_invariants",
})


class ConfigDrift:
    """One variable the recording and this process disagree about.

    Only `changed` is drift. The other two are *registration* differences, and
    they are reported without failing anything:

    A variable exists in a process only once the module declaring it has been
    imported, so `missing` means no code in this process ever read it -- which
    means it cannot have influenced this process, whatever it was set to over
    there. `added` is the same fact from the other side. Where a difference of
    that kind did change the recorded game, the per-step digest catches it,
    which is the oracle these reports exist to keep interpretable.

    Failing on them would fire on nothing but import order. See MARVEL-34.
    """

    # `changed`  both have it, with different values          -- drift
    # `missing`  the recording has it, this process did not   -- unmatched
    # `added`    this process has it, the recording did not   -- unmatched
    KIND: TypeAlias = Literal["changed", "missing", "added"]

    FAILING: Tuple['ConfigDrift.KIND', ...] = ("changed",)

    def __init__(self, name: str, kind: 'ConfigDrift.KIND',
                 recorded: Any=None, current: Any=None) -> None:
        self.name = name
        self.kind = kind
        self.recorded = recorded
        self.current = current

    def __repr__(self) -> str:
        return f"{self.name}: {self.Describe()}"

    @property
    def is_failing(self) -> bool:
        return self.kind in ConfigDrift.FAILING

    def Describe(self) -> str:
        if self.kind == "changed":
            return f"recorded {self.recorded!r}, running {self.current!r}"
        if self.kind == "missing":
            return (f"recorded {self.recorded!r}, never registered here "
                    f"(nothing in this process read it)")
        return (f"not recorded, running {self.current!r} "
                f"(nothing in the generating process read it)")

    def ToDict(self) -> Dict[str, Any]:
        return {
            "name": self.name,
            "kind": self.kind,
            "recorded": self.recorded,
            "current": self.current,
        }


class ConfigRecord:

    VERSION = 1

    ################################################################################
    #
    @staticmethod
    def IsCompared(name: str) -> bool:
        """Whether a difference in this variable means the corpus is suspect."""
        if name in INVOCATION:
            return False
        if name.startswith(ROLE_PREFIXES):
            return False
        if name.endswith(PATH_SUFFIXES):
            return False
        return True

    @staticmethod
    def Values() -> Dict[str, Any]:
        """Every registered variable, resolved. Sorted, so the output is stable."""
        from engine.config import ConfigVariables

        return {name: ConfigRecord.Plain(ConfigVariables.variable_dict[name].value)
                for name in sorted(ConfigVariables.variable_dict)}

    @staticmethod
    def Plain(value: Any) -> Any:
        """JSON-safe, and a copy: a manifest must not alias live config state."""
        if isinstance(value, list):
            return [ConfigRecord.Plain(item) for item in value]
        if isinstance(value, (str, int, float, bool)) or value == None:
            return value
        return str(value)

    @staticmethod
    def Snapshot() -> Dict[str, Any]:
        """What this process resolved its configuration to.

        `compared` travels with the values on purpose. It is the policy that was
        in force when the corpus was made, so a verifier years later reports
        against the rules the recording was written under rather than silently
        applying its own.
        """
        from engine.config import ConfigVariables

        values = ConfigRecord.Values()
        return {
            "version": ConfigRecord.VERSION,
            "git_sha": ConfigRecord.GitSha(),
            "sources": {name: ConfigVariables.variable_dict[name].set_from
                        for name in values},
            "values": values,
            "compared": [name for name in values if ConfigRecord.IsCompared(name)],
        }

    ################################################################################
    #
    @staticmethod
    def Compare(recorded: Dict[str, Any]|None) -> List['ConfigDrift']:
        """Differences between a recorded snapshot and this process.

        The comparison runs over the union of what each side calls comparable,
        so a variable that this process has and the recording does not is still
        reported -- a flag added since the corpus was generated is exactly the
        kind of drift worth knowing about.

        A snapshot this code does not understand is not silently accepted: an
        unreadable or newer `version` returns a single drift saying so.
        """
        # Keyed on the field being *present*, not on it being non-empty: a
        # manifest that recorded nothing is a different thing from one written
        # before there was anything to record, and only the second is an error.
        if not isinstance(recorded, dict) or "values" not in recorded:
            return [ConfigDrift("(snapshot)", "changed",
                                "a config snapshot", "nothing readable")]

        version = recorded.get("version")
        if not isinstance(version, int) or version > ConfigRecord.VERSION:
            return [ConfigDrift("(snapshot version)", "changed",
                                version, ConfigRecord.VERSION)]

        recorded_values: Dict[str, Any] = recorded["values"]
        current_values = ConfigRecord.Values()

        # What the recording called comparable, falling back to this process's
        # rules for a snapshot written before `compared` existed.
        names = set(recorded.get("compared")
                    or [name for name in recorded_values
                        if ConfigRecord.IsCompared(name)])
        names |= {name for name in current_values if ConfigRecord.IsCompared(name)}

        drifts: List['ConfigDrift'] = []
        for name in sorted(names):
            in_recorded = name in recorded_values
            in_current = name in current_values
            if in_recorded and in_current:
                if recorded_values[name] != current_values[name]:
                    drifts.append(ConfigDrift(name, "changed",
                                              recorded_values[name],
                                              current_values[name]))
            elif in_recorded:
                drifts.append(ConfigDrift(name, "missing",
                                          recorded=recorded_values[name]))
            else:
                drifts.append(ConfigDrift(name, "added",
                                          current=current_values[name]))
        return drifts

    ################################################################################
    #
    @staticmethod
    def GitSha() -> str|None:
        """The checked-out commit, or None outside a git checkout.

        Read straight out of `.git` rather than by running `git`, so it needs no
        subprocess and no git on the machine -- and so it stays as reproducible
        as everything else in the manifest.

        It says which commit is checked out and **not** whether the tree is
        clean. A corpus generated from a modified working tree records the
        commit it was modified from, which is the best a file can do; the
        engine version beside it is what actually gates loading a scene.
        """
        import os

        git_dir = ConfigRecord.FindGitDir()
        if not git_dir:
            return None

        head = ConfigRecord.ReadTextFile(os.path.join(git_dir, "HEAD"))
        if not head:
            return None
        if not head.startswith("ref:"):
            # Detached: HEAD holds the object name itself.
            return head if ConfigRecord.IsSha(head) else None

        ref = head[len("ref:"):].strip()
        # A linked worktree keeps HEAD of its own but shares refs with the main
        # checkout, so the ref may only exist in the common directory.
        for base in ConfigRecord.RefBases(git_dir):
            value = ConfigRecord.ReadTextFile(os.path.join(base, *ref.split("/")))
            if value and ConfigRecord.IsSha(value):
                return value
            packed = ConfigRecord.ReadPackedRef(os.path.join(base, "packed-refs"), ref)
            if packed:
                return packed
        return None

    @staticmethod
    def RefBases(git_dir: str) -> List[str]:
        """Directories a ref might live in: this git dir, then the common one."""
        import os

        bases = [git_dir]
        common = ConfigRecord.ReadTextFile(os.path.join(git_dir, "commondir"))
        if common:
            if not os.path.isabs(common):
                common = os.path.normpath(os.path.join(git_dir, common))
            bases.append(common)
        return bases

    @staticmethod
    def FindGitDir() -> str|None:
        """Walk up from this file for a `.git`, resolving the worktree form."""
        import os

        folder = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
        while True:
            candidate = os.path.join(folder, ".git")
            if os.path.isdir(candidate):
                return candidate
            if os.path.isfile(candidate):
                # A linked worktree: `.git` is a file holding `gitdir: <path>`.
                line = ConfigRecord.ReadTextFile(candidate)
                if not line or not line.startswith("gitdir:"):
                    return None
                target = line[len("gitdir:"):].strip()
                if not os.path.isabs(target):
                    target = os.path.normpath(os.path.join(folder, target))
                return target if os.path.isdir(target) else None
            parent = os.path.dirname(folder)
            if parent == folder:
                return None
            folder = parent

    @staticmethod
    def ReadTextFile(path: str) -> str|None:
        try:
            with open(path, "r", encoding="utf-8") as file:
                return file.read().strip()
        except OSError:
            return None

    @staticmethod
    def ReadPackedRef(path: str, ref: str) -> str|None:
        text = ConfigRecord.ReadTextFile(path)
        if not text:
            return None
        for line in text.splitlines():
            if line.startswith(("#", "^")):
                continue
            parts = line.split(" ", 1)
            if len(parts) == 2 and parts[1].strip() == ref and ConfigRecord.IsSha(parts[0]):
                return parts[0]
        return None

    @staticmethod
    def IsSha(value: str) -> bool:
        return len(value) == 40 and all(c in "0123456789abcdef" for c in value)
