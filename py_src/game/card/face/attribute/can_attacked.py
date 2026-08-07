from . import *

# For "54035"
class CanAttacked(CardFace):

    def TakeDamageWithOverkillTarget(self, source: 'CardFace', property: 'int|DamageProperty', by_effect: 'Effect',
                                    would_attack_unit_message: 'Message.WhenUnitWouldAttackUnit|None'=None,
                                    overkill_target: 'Unit2|None'=None,
                                    *,
                                    from_atk_message: 'Message.WhenUnitWouldAttack|None'=None, # Fix for indirect damage, "43036"
                                    operation: Callable[['Message.AfterUnitTookDamage'], None]|None=None,
                                    ) -> List['Message.AfterUnitDefeatedUnit|Message.AfterUnitTookDamage']:
        self.TakeDamage(source, property, by_effect, would_attack_unit_message, operation=operation)
        return []

    def TakeDamage(self, source: 'CardFace', property: 'int|DamageProperty', by_effect: 'Effect',
                    would_attack_unit_message: 'Message.WhenUnitWouldAttackUnit|None'=None,
                    *,
                    operation: Callable[['Message.AfterUnitTookDamage'], None]|None=None,
                    ) -> 'Message.AfterUnitDefeatedUnit|Message.AfterUnitTookDamage|None':
        if isinstance(property, int):
            damage = property
        else:
            damage = property.damage

        if CanPlaceCounter.IsType(self):
            self.PlaceCountersInternal(damage, 'damage', by_effect)

    def ResolveRetaliate(self, atk_message: 'Message.WhenUnitWouldAttackUnit'):
        pass

    def DoAttackYou(self, against_player: 'Player', by_effect: 'Effect', *,
                    property: 'AttackProperty|None'=None,
                    operation: Callable[['Message.WhenUnitWouldAttack'], Any]|None=None,
                    if_no_character_takes_damage: Callable[[], None]|None=None,
                    you_cannot_play_event: bool=False
                    ) -> 'Message.AfterUnitAttackEnd|None':
        return None

