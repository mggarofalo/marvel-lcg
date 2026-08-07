from . import *

# Sure Gamble

def GetAbilities() -> Sequence['Ability']:

    def sure_gamble(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Worlds.UpdateNextCardPlayCost(
            "Any",
            -3,
            effect,
            in_this="Phase"
        )

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            sure_gamble
        ).SetPlay().SetLabel(),
    ]

