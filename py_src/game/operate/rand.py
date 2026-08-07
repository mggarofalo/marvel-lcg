from . import *

class Rand:

    @staticmethod
    def IsDisableShuffle(effect: 'Effect|World') -> 'bool':
        from game.world import World
        if isinstance(effect, World):
            world = effect
        else:
            world = effect.world
        return world.rule.disable_shuffle

    T = TypeVar("T")
    @staticmethod
    def RandomChoice(input_list: Sequence[T], effect: 'Effect|World') -> T:
        assert input_list != []
        if Rand.IsDisableShuffle(effect):
            return input_list[0]
        return Random.RandomChoice(input_list)

    @staticmethod
    def RandomChoice2(input_list: Sequence[T], x: int, effect: 'Effect|World') -> List[T]:
        if Rand.IsDisableShuffle(effect):
            return [input_list[0]]
        return Random.RandomChoice2(input_list, x)

    @staticmethod
    def Shuffle(list: List[Any], effect: 'Effect') -> None:
        if Rand.IsDisableShuffle(effect):
            return
        return Random.Shuffle(list)

