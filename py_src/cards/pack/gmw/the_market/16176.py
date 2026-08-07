from . import *

# Tried and True

def GetAbilities() -> Sequence['Ability']:

    def tried_and_true(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        for target in effect.targets:
            player = target.GetControlByPlayer()
            player.MayChooseOneAbility(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        Faces.AddToHand(targets, player, effect)
                ).SetTarget(from_where=["YourDiscardPile"], range=(1, 3))
            )
        Faces.RemoveAllFromGame([this], effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            tried_and_true
        ).SetPlay().SetLabel()
        .SetTarget("Players")
        .SetHasNoTargetEffect(),
    ]

