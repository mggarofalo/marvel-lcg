from . import *

# Game Time

def GetAbilities() -> Sequence['Ability']:

    def game_time(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)
        this.HealthUnits(effect.targets, 1, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            game_time
        ).SetPlay().SetLabel()
        .SetTarget(
            CardFinder(card_type=Ally, with_attach=CardFinder2("TRAINING", Upgrade), canbe_ready=True)|
            CardFinder(card_type=Ally, with_attach=CardFinder2("TRAINING", Upgrade), canbe_heal=True)
        ),
    ]

