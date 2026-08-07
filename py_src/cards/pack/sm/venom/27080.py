from . import *

# Lashing Out

def GetAbilities() -> Sequence['Ability']:

    def lashing_out_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetToPlayer()
        player.GetIdentity().TakeIndirectDamage(this, 2, effect)


    return [
        AfterVenomTakesAttackDamageRemoveEqualThreat(),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            lashing_out_boost,
            during_attack=True,
        ),
    ]

