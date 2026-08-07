from core import *
from game.effect import *

@dataclass
class CommandDescriptor:
    id: str = field(default='')
    targets: List[str] = field(default_factory=lambda: [])
    resources: List[str] = field(default_factory=lambda: [])

    def GetDebugCommand(self) -> str:
        id = str(self.id)
        if id.startswith("Puzzle."):
            return self.id
        if id.startswith(":"):
            return self.id[1:]
        return ''

    @staticmethod
    def FindNewEffectId(effect_str: str, check_effect_list: Sequence['Effect']) -> int:
        id = CommandDescriptor.FindNewEffectIdInternal(effect_str, check_effect_list)
        assert id
        return id[0]

    @staticmethod
    def FindNewEffectIdInternal(effect_str: str, check_effect_list: Sequence['Effect']) -> List[int]:
        import re
        found_effects: List['Effect'] = []
        m = re.match(r"e(\d+) (.*) c(\d+) .*", effect_str)
        if m:
            effect_name = m[2].replace(" ", "_")
            object_id = int(m[3])
        else:
            m = re.match(r"e(\d+) c(\d+) .*", effect_str)
            assert m
            effect_name = ""
            object_id = int(m[2])

        # We cannot simply return `m[1]`

        for effect in check_effect_list:
            if effect.this.card.object_id == object_id:
                found_effects.append(effect)

        if len(found_effects) == 1:
            pass
        else:
            for effect in found_effects[:]:
                # Fix "10029", "42002"
                if effect.GetDisplayName(remove_space=True) != effect_name:
                    found_effects.remove(effect)
        return [x.object_id for x in found_effects]

    @staticmethod
    def FromDebugCommand(cmd: str) -> 'CommandDescriptor':
        if cmd.startswith("Puzzle."):
            pass
        else:
            cmd = f":{cmd}"
        return CommandDescriptor(cmd, [], [])

def CardEffectInt(card_name_id: str|int) -> int:
    if card_name_id == '':
        return 0
    if isinstance(card_name_id, int):
        return card_name_id
    if card_name_id.startswith(":"):
        return 0

    import re
    m = re.match(r"[ec](\d+) .+", card_name_id)
    if m:
        return int(m[1])

    m = re.match(r"{'id': (\d+),.+", card_name_id)
    if m:
        return int(m[1])

    return int(card_name_id)


@dataclass
class OperationDescriptor:
    step: int = field(default=-1)
    event: str = field(default="")
    effect: CommandDescriptor = field(default_factory=lambda:CommandDescriptor())
    crc: str = field(default="")
    # Version 5
    # hash: str = field(default="")

