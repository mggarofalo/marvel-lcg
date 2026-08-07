from . import *

# Sonic Bubble

def GetAbilities() -> Sequence['Ability']:

    def sonic_bubble(effect: 'Effect', message: 'Message.WhenFaceWouldDealDamage') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        message.SetBeInstead(effect)

        value = message.will_deal_damage
        this.RemoveThreatFromSchemes([this], value, effect)


    return [
        AbilityFactory.WhenDamageWouldBeDealtTo(
            AbilityType.ForcedInterrupt,
            Enemy,
            sonic_bubble
        ),
    ]

