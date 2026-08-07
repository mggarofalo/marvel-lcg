from . import *

# Extreme Measures

def GetAbilities() -> Sequence['Ability']:

    def extreme_measures(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.trigger.GetControlByPlayer()
        damage = FacesCounter.GetPrintedCost([message.trigger])

        player.GetIdentity().TakeIndirectDamage(this, damage, effect)


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.ForcedResponse,
            PlayerCard,
            extreme_measures
        ),
    ]

