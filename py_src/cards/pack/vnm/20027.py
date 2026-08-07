from . import *

# "Welcome Aboard"

def GetAbilities() -> Sequence['Ability']:

    def welcome_aboard(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Worlds.UpdateNextCardPlayCost(
            "Any",
            -2,
            effect,
            finder=CardFinder(card_type=Ally),
            in_this="Phase"
        )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            welcome_aboard,
        ).SetPlay(only_if_your_identity_has_trait="GUARDIAN").SetLabel()
        .MaxOnePerRound(),
    ]

