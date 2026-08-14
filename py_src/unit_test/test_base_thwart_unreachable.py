"""Base THW is not grantable, and these guards fail when someone makes it so.

`HasThwart.SetBaseThwart` is the fourth base-statistic setter and the only one
without the `PushBaseX`/`PopBaseX` grant stack MARVEL-111 gave the other three.
The reason is that it has no caller: no card in the game grants a base THW, so
no plumbing carries one. `ModelGain.Gain` and the two ability factories above it
take `base_sch`, `base_atk` and `base_health` and nothing for thwart, because
"has a base SCH of 1, a base ATK of 1, and a base hit points of 1" is the only
base-grant text ever printed -- Ultron Drones (`01140`, reprinted `26031`) and
Controlled Innocents (`50032`) -- and it is aimed at facedown minions, which have
a SCH and no THW. Hope Summers (`40130`) is the one card whose text says "base
THW"; it says *equal to* the hero's rather than a number, she prints no THW, and
she is implemented as an ordinary `thwart=` keyword gain.

That decision is only safe while it stays true, and a docstring cannot tell when
it stops being true. What reintroduces MARVEL-111 is not forgetting the stack --
it is the next author threading `base_thw` through the plumbing and calling
`SetBaseThwart` on application while passing `printed_thwart` back on removal,
which is exactly the shape the other three had. Note that giving `SetBaseThwart`
a grant stack today would *not* prevent that: the setter would still be there,
still public, still the obvious thing to call. A guard that fires on the
plumbing does prevent it.

So there are two guards, one per half of that plumbing:

- nothing outside `can_thwart.py` calls `SetBaseThwart`;
- no base-thwart parameter exists on the three functions that carry the other
  three base statistics from a card script down to `Gain`.

**Both are meant to be deleted, not worked around.** If you are making base THW
grantable, give `HasThwart` the `PushBaseThwart`/`PopBaseThwart` pair its three
counterparts have, clear the stack in `OnResetKeywords` beside `base_thwart`,
have the new plumbing call the pair rather than the setter, port the coverage in
`unit_test/test_base_stat_grant.py` to thwart -- including
`TestOrderingNeedsThreeGrants`, since two live grants cannot tell "fall back to
the newest" from "fall back to the oldest" -- and then remove this file.

See MARVEL-113, and MARVEL-111 for what the stack is for.
"""

import ast
import inspect
import pathlib
import unittest

# `engine` first, and not for its side effects: the `game.*` modules import each
# other in a cycle that only resolves once `engine/__init__.py` has walked it.
import engine  # noqa: F401  pylint: disable=unused-import


SETTER = "SetBaseThwart"

PY_SRC = pathlib.Path(__file__).resolve().parent.parent

# `can_thwart.py` defines the setter, so it is allowed to name it. `unit_test/`
# is excluded because a test calling the setter is testing it, not shipping a
# path to it -- this file names it too.
OWNER = PY_SRC / "game" / "card" / "face" / "attribute" / "can_thwart.py"
SKIPPED_DIRS = (".venv", "__pycache__", "unit_test")

HOW_TO_FIX = (
    f"{SETTER} has no grant stack because it has no caller (MARVEL-113). If you "
    f"are making base THW grantable, give HasThwart PushBaseThwart/PopBaseThwart "
    f"like the other three base statistics, clear the stack in OnResetKeywords, "
    f"call the pair instead of the setter, and delete this guard -- do not "
    f"silence it. Calling the setter on application and restoring printed_thwart "
    f"on removal is MARVEL-111 again."
)


def SourceFiles():
    """Every shipped `.py` under `py_src/` that is allowed to reach the setter."""
    for path in PY_SRC.rglob("*.py"):
        if any(part in SKIPPED_DIRS for part in path.parts):
            continue
        if path == OWNER:
            continue
        yield path


def ReferencesInSource(text: str, name: str):
    """Lines in `text` that *use* `name` -- attribute, bare name, or string.

    A `def name(...)` is not a use, which is why `ast.FunctionDef` is not
    matched: the file that defines the setter must not count as reaching it.
    The string case is what catches `getattr(unit, "SetBaseThwart")`, which no
    search for a call would see.
    """
    if name not in text:
        # The parse is the expensive half and 4400 files do not need it.
        return []

    try:
        tree = ast.parse(text)
    except SyntaxError:  # pragma: no cover - a broken file is another test's job
        return []

    hits = []
    for node in ast.walk(tree):
        if isinstance(node, ast.Attribute) and node.attr == name:
            hits.append(node.lineno)
        elif isinstance(node, ast.Name) and node.id == name:
            hits.append(node.lineno)
        elif isinstance(node, ast.Constant) and node.value == name:
            hits.append(node.lineno)
        elif isinstance(node, ast.alias) and name in (node.name, node.asname):
            hits.append(node.lineno)
    return sorted(set(hits))


def ReferencesTo(path: pathlib.Path, name: str):
    return ReferencesInSource(
        path.read_text(encoding="utf-8", errors="replace"), name)


def BaseThwartParameters(func):
    """Parameters of `func` that would carry a granted base THW."""
    names = inspect.signature(func).parameters
    return [n for n in names if n.startswith("base_") and "thw" in n]


class TestNothingCallsTheSetter(unittest.TestCase):

    def test_the_setter_has_no_call_site(self):
        found = {}
        for path in SourceFiles():
            lines = ReferencesTo(path, SETTER)
            if lines:
                found[path.relative_to(PY_SRC).as_posix()] = lines

        self.assertEqual(found, {}, HOW_TO_FIX)

    def test_the_setter_still_exists_under_that_name(self):
        """The guard searches for a *string*, so the string has to still be real.

        Every assertion in this file is "nobody names `SetBaseThwart`", and a
        rename satisfies all of them at once: the scan finds nothing, the
        controls below go on passing because they test the reader rather than
        the engine, and the guard protects a method that no longer exists.
        Renaming a setter is an ordinary thing to do -- the C# port will rename
        plenty -- so the name is pinned here rather than assumed.
        """
        from game.card.face.attribute.can_thwart import HasThwart

        self.assertTrue(
            hasattr(HasThwart, SETTER),
            f"HasThwart has no {SETTER}. If it was renamed, rename SETTER with "
            f"it; if it was deleted, delete this guard. Leaving them apart "
            f"makes every assertion in this file vacuous.")

    def test_the_scan_can_see_a_call_site(self):
        """The guard above is a claim about absence, so prove it can find one.

        Without this, a `ReferencesInSource` that returns `[]` for everything --
        a mistyped node class, a prefilter that never matches -- leaves a guard
        that passes forever and protects nothing. The three shapes below are the
        three a real reintroduction could take.
        """
        called = "def f(unit, effect):\n    unit.SetBaseThwart(1, effect)\n"
        self.assertEqual(ReferencesInSource(called, SETTER), [2])

        imported = "from x import SetBaseThwart\nSetBaseThwart(1, None)\n"
        self.assertEqual(ReferencesInSource(imported, SETTER), [1, 2])

        reflected = 'getattr(unit, "SetBaseThwart")(1, effect)\n'
        self.assertEqual(ReferencesInSource(reflected, SETTER), [1])

        # ...and the reader is joined to the file, not to a constant. This file
        # holds the name as a string, so scanning it must find something.
        self.assertTrue(
            ReferencesTo(pathlib.Path(__file__), SETTER),
            "ReferencesTo is not reading the file it was handed")

    def test_the_scan_covers_where_a_call_site_would_appear(self):
        """A skip list is the cheapest way to make the guard vacuous.

        Widening `SKIPPED_DIRS` until nothing is scanned leaves every assertion
        above still passing. The two places a base-THW call site would actually
        be written -- the plumbing, and a card script -- are named here so that
        dropping either from the scan fails rather than quietly narrows it.
        """
        scanned = {p.relative_to(PY_SRC).as_posix() for p in SourceFiles()}

        self.assertIn("game/card/face/model/face_gain.py", scanned)
        self.assertIn("cards/pack/core/ultron/01140.py", scanned)
        self.assertNotIn(OWNER.relative_to(PY_SRC).as_posix(), scanned)

    def test_defining_the_setter_is_not_reaching_it(self):
        """`can_thwart.py` names it in a `def`, and that must not count.

        If it did, the guard could only be satisfied by deleting the setter, and
        would be excluding its owner for the wrong reason.
        """
        defined = f"class C:\n    def {SETTER}(self, value, by_effect):\n        pass\n"
        self.assertEqual(ReferencesInSource(defined, SETTER), [])


class TestNoPlumbingCarriesABaseThwart(unittest.TestCase):
    """The three functions that carry `base_sch`/`base_atk`/`base_health`.

    A card script reaches `Gain` through both factories, so a base THW would have
    to appear on all three. Any one of them is enough to fail.
    """

    def test_gain_takes_no_base_thwart(self):
        from game.card.face.model.face_gain import ModelGain

        self.assertEqual(BaseThwartParameters(ModelGain.Gain), [], HOW_TO_FIX)

    def test_the_ability_factory_takes_no_base_thwart(self):
        from game.ability.factory.environment import AbilityFactoryEnvironment

        factory = AbilityFactoryEnvironment.GiveKeywordToInPlayWhenApplyThis
        self.assertEqual(BaseThwartParameters(factory), [], HOW_TO_FIX)

    def test_the_factory_helper_takes_no_base_thwart(self):
        from game.ability.factory.environment_helper import (
            GiveKeywordToInPlayWhenApplyThisInternal)

        self.assertEqual(
            BaseThwartParameters(GiveKeywordToInPlayWhenApplyThisInternal),
            [], HOW_TO_FIX)

    def test_the_signature_check_can_see_a_base_thwart(self):
        """As above: a reader that matches nothing looks exactly like a pass.

        The second half is the distinction the check has to draw. `thwart=` and
        `thwart_consequential_damage=` are ordinary keyword gains -- Hope
        Summers' whole implementation is the former -- and neither is a base
        grant. Only a `base_` parameter is.
        """
        def granted(effect, diff, base_sch=None, base_thw=None): ...
        self.assertEqual(BaseThwartParameters(granted), ["base_thw"])

        def spelt_out(effect, diff, base_thwart=None): ...
        self.assertEqual(BaseThwartParameters(spelt_out), ["base_thwart"])

        def clean(effect, diff, thwart=None, thwart_consequential_damage=None): ...
        self.assertEqual(BaseThwartParameters(clean), [])

    def test_the_other_three_base_statistics_are_still_carried(self):
        """The signature check is a claim about absence too.

        `BaseThwartParameters` matching nothing at all -- a typo'd prefix, a
        signature the check reads off the wrong object -- looks identical to a
        clean result. The three that *are* plumbed say the reader works.
        """
        from game.card.face.model.face_gain import ModelGain

        names = inspect.signature(ModelGain.Gain).parameters
        carried = sorted(n for n in names if n.startswith("base_"))
        self.assertEqual(carried, ["base_atk", "base_health", "base_sch"])
