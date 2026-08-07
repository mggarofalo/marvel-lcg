from . import *

# Weather Goddess

def GetAbilities() -> Sequence['Ability']:

    def weather_goddess(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        SwapYourWeatherSupport(initiator, effect.targets, True, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            weather_goddess
        ).SetPlay().SetLabel()
        .SetTarget(GetWeatherDeck),
    ]

