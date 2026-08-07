from . import *

# "Go for Champions!"

def GetAbilities() -> Sequence['Ability']:

    def go_for_champions(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.effect.RegisterTemp(
            AbilityFactory.UnitCannotTakeDamageWhile(
                AbilityType.Temp0,
                CardFinder2("CHAMPION", Unit2),
            ),
            unregister_after_exec=False,
            until_round_end=True
        )

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            go_for_champions,
        ).SetPlay(only_if_your_identity_has_trait="CHAMPION").SetLabel()
        .SetCostFunc(CostFunc.RemoveFromGame("This")),
    ]

