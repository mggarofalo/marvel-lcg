from . import *

# * Wildside

def GetAbilities() -> Sequence['Ability']:

    def wildside_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Wildside attacks you",
                lambda targets:
                    this.DoAttackYou(player, effect)
            ),
            AbilityFactory.ForChoiceAbility(
                "Return the highest-cost support you control to its owner's hand",
                lambda targets:
                    Faces.ReturnToHand(targets, "Owner", effect)
            ).SetTarget(Support, highest_cost=True, from_where=["YouControlCards"]),
        )


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            wildside_revealed
        ),
    ]

