from . import *

# * Greycrow

def GetAbilities() -> Sequence['Ability']:

    def greycrow(effect: 'Effect', message: 'Message.WhenUnitWouldAttackUnit') -> None:
        this = effect.this.CastTo(Enemy)
        Unused(this)

        player = message.target.GetControlByPlayer()

        faces = player.GetControlCards()
        faces = Filter.All(faces, HasCost, highest_cost=True, max_size=1)
        value = FacesCounter.GetPrintedCost(faces)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Discard each card you control with the highest cost",
                lambda targets:
                    Faces.DiscardAll(targets, effect)
            ).SetTarget(canbe_discard=True, highest_cost=True, range="All", from_where=["YouControlCards"]),
            AbilityFactory.ForChoiceAbility(
                f"Greycrow gets +{value} ATK for this attack",
                lambda targets:
                    message.GainAttackForThisAttack(+value, effect)
            )
        )


    return [
        AbilityFactory.WhenUnitWouldAttackUnit(
            AbilityType.ForcedInterrupt,
            "This",
            CardFinder(card_type=Identity|Ally),
            greycrow
        ),
    ]

