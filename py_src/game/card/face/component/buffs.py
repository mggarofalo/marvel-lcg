from . import *

class Buffs(Component):

    TB = TypeVar("TB", bound=Buff)

    @override
    def __init__(self, card: 'Card') -> None:
        super().__init__(card)
        self.buffers: Dict[Type['Buffs.TB'], 'Buff'] = {}

    def GetBuff(self, type: Type['TB']) -> 'TB':
        if type not in self.buffers:
            self.buffers[type] = self.parent.world.buff_manager.RegisterBuffer(type)
        return Cast(type, self.buffers[type])

    def GetBuffs(self) -> Sequence['Buff']:
        return [self.buffers[x] for x in self.buffers]

