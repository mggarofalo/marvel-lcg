from . import *

# Hull Breach

def GetAbilities() -> Sequence['Ability']:

    def hull_breach_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        player.ChooseAbilities(
            effect,
            ExhaustTheMilanoForChoiceAbility(effect),
            AbilityFactory.ForChoiceAbilityWithCost(
                Cost("BB"),
            ),
            AbilityFactory.ForChoiceAbility(
                "Deal 3 damage to the first player",
                lambda targets:
                    this.DealDamage(targets, 3, effect),
            ).SetTarget("FirstPlayer"),
        )


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            hull_breach_revealed
        ),
    ]

