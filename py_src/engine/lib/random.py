"""The generator the game draws from. Specified in `docs/rng-contract.md`.

One MT19937 stream, seeded once per game from the scene. There is no longer a
choice of backend: `disable_numpy_random` and the numpy dependency are gone,
because "which generator" cannot be a runtime option in something the C# engine
has to reproduce draw for draw.
"""

from core import *
from engine.lib.mt19937 import Mt19937

CATEGORY_NAME = "RANDOM"

class Random:
    seed = 0
    counter = 0
    rand = Mt19937()

    @staticmethod
    def AddCounter():
        from engine.log import Log
        Random.counter += 1
        Log.DebugSilent(CATEGORY_NAME, f"{Random.counter=}")

    @staticmethod
    def SetSeed(seed: int) -> None:
        from engine.log import Log
        Random.seed = seed
        Random.counter = 0
        Log.DebugSilent(CATEGORY_NAME, f"Seed: {seed}")
        Random.rand.Seed(seed)

    @staticmethod
    def RandomSeed() -> int:
        """Pick a seed when the scene has none. The only unseeded draw there is.

        `GameSession.GameSetup` writes the result back into the scene before
        anything uses it, so the game is still reproducible from the saved
        replay -- this decides *which* game gets played, not how it unfolds.
        """
        import random
        seed = random.randrange(2**31-2)+1
        Random.SetSeed(seed)
        return seed

    T = TypeVar("T")
    @staticmethod
    def RandomChoice(input_list: Sequence[T]) -> T:
        assert input_list != []
        Random.AddCounter()
        return Random.rand.Choice(input_list)

    @staticmethod
    def RandomChoice2(input_list: Sequence[T], x: int) -> List[T]:
        if x < 0:
            raise ValueError("x cannot be negative.")
        if x > len(input_list):
            raise ValueError("x cannot be greater than the length of the input list.")

        # Selecting every element is not a random choice -- there is one
        # answer -- so it returns the input order and consumes no randomness.
        # Inherited behaviour, kept deliberately, and part of the contract that
        # C# reproduces. See `docs/rng-contract.md` section 6.
        if x == len(input_list):
            return list(input_list)
        if x == 0:
            return []

        Random.AddCounter()
        return Random.rand.ChooseWithoutReplacement(input_list, x)

    @staticmethod
    def Shuffle(list: List[Any]) -> None:
        Random.AddCounter()
        Random.rand.Shuffle(list)

    @staticmethod
    def Undo():
        """Debug console only (`Unshuffle`). Not part of the C# contract."""
        Random.rand.Undo()
