from . import *

# * Hulk

def GetAbilities() -> Sequence['Ability']:

    def hulk(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Ally)

        initiator = effect.GetInitiator()
        face = initiator.DiscardDeckTopCard(effect)

        resource = FacesCounter.GetPrintedResources([face])

        def deal_2_damange_to_an_enemy(this: Ally):

            initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "Deal 2 damage to an enemy",
                    lambda targets:
                        this.DealDamage(targets, 2, effect)
                ).SetTarget(Enemy)
            )

        def deal_1_damange_to_each_character(this: Ally):

            initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "Deal 1 damage to echo character",
                    lambda targets:
                        this.DealDamage(targets, 1, effect)
                ).SetTarget(Unit2, range="All")
            )

        def discard_hulk(this: Ally):
            Faces.DiscardAll([this], effect)

        if resource.g > 0:
            deal_2_damange_to_an_enemy(this)
            deal_1_damange_to_each_character(this)
            discard_hulk(this)
        elif resource.r > 0:
            deal_2_damange_to_an_enemy(this)
        elif resource.y > 0:
            deal_1_damange_to_each_character(this)
        elif resource.b > 0:
            discard_hulk(this)


    return [
        AbilityFactory.ThisCanThwart(
            conditions=False
        ),
        AbilityFactory.AfterUnitAttackEnd(
            AbilityType.ForcedResponse,
            "This",
            hulk,
        )
    ]
