from . import *

# Running Interference

def GetAbilities() -> Sequence['Ability']:

    def running_interference(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        value = 2
        villain = Worlds.FindVillain(effect)
        if villain:
            value += min(3, villain.printed_stage)

        this.RemoveThreatFromSchemes(effect.targets, value, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            running_interference
        ).SetPlay(only_if_your_identity_has_trait="AVENGER").SetLabel('thwart')
        .SetTarget(MainScheme),
    ]

