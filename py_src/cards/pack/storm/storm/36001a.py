from . import *

# * Storm

def GetAbilities() -> Sequence['Ability']:

    def storm(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        initiator = effect.GetInitiator()
        SwapYourWeatherSupport(initiator, effect.targets, True, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            storm
        ).SetTarget(GetWeatherDeck)
        .LimitOncePerRound()
        .SetName("Weather Control"),
    ]

