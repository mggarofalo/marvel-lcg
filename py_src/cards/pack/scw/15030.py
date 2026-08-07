from . import *

# Bait and Switch

def GetAbilities() -> Sequence['Ability']:

    def bait_and_switch(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        villain = Worlds.FindVillain(effect)
        if villain:
            villain.DoAttackYou(initiator, effect)
        this.RemoveThreatFromSchemes(effect.targets, 4, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            bait_and_switch
        ).SetPlay().SetLabel('thwart')
        .SetTarget(MainScheme)
    ]

