from . import *

# Aerial Dogfight

def GetAbilities() -> Sequence['Ability']:

    def aerial_dogfight(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> bool:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        attacker = message.attacker
        if attacker and attacker.HasTrait("AERIAL"):
            return False

        if message.by_effect.this.HasTrait("AERIAL"):
            return False

        if message.would_atk_message and \
            message.would_atk_message.property.ranged:
            return False

        return True


    return [
        AbilityFactory.ReduceCharacterTakeDamageBy(
            CardFinder2("AERIAL", Unit2),
            2,
            from_each_attack=True,
            conditions=[aerial_dogfight]
        ),
    ]

