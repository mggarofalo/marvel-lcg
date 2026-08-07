from core import *

@dataclass
class DamageProperty:
    damage: int = field(default=0)
    ignore_tough: bool = field(default=False)
    is_from_overkill: bool = field(default=False)
    is_indirect_damage: bool = field(default=False)

    def Copy(self) -> 'DamageProperty':
        return DamageProperty(
            damage=self.damage,
            ignore_tough=self.ignore_tough,
            is_from_overkill=self.is_from_overkill,
            is_indirect_damage=self.is_indirect_damage
        )

