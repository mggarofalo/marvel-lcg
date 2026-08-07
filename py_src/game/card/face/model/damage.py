from core import *
from game.card.face import *
from game.effect import *
from game.card.face.model.base import ModelBase
from game.element.damage_property import DamageProperty

class ModelDamage(ModelBase):

    @final
    def DefaultDealDamageTo(self, source: 'CardFace', units: List['Unit2'], damage: 'int|DamageProperty', by_effect: 'Effect') -> int|None:
        dealt_damage = 0

        from game.operate.faces_helper import FacesHelper
        units_dict = FacesHelper.ListToDict(units)

        if isinstance(damage, int):
            damage = DamageProperty(damage)

        for unit in units_dict:
            new_damage = damage.Copy()
            new_damage.damage = damage.damage * units_dict[unit]
            damage_message = unit.TakeDamage(source, new_damage, by_effect)
            if damage_message:
                dealt_damage += damage_message.deal_damage
        return dealt_damage

    def DefaultRemoveSchemeThreat(self, by_face: 'CardFace', schemes: List['Scheme2'], value: int|Literal["All"], by_effect: 'Effect') -> int|None:
        from game.operate.faces_helper import FacesHelper
        total_value = 0
        schemes_dict = FacesHelper.ListToDict(schemes)
        for scheme in schemes_dict:
            if value == "All":
                remove_value = "All"
            else:
                remove_value = schemes_dict[scheme] * value
            total_value += scheme.RemoveThreatInternal(by_face, remove_value, by_effect)
        return total_value

