from . import *

# * Adam Warlock

def GetAbilities() -> Sequence['Ability']:

    def adam_warlock(effect: 'Effect', message: 'Message.AfterUnitAttackUnit|Message.AfterUnitThwartEnd') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        faces = initiator.DiscardRandomHandCards(1, effect)
        if faces:
            res = FacesCounter.GetPrintedResources(faces)

            ability_r = AbilityFactory.ForChoiceAbility(
                "Remove 3 threat from a scheme",
                lambda targets:
                    this.RemoveThreatFromSchemes(targets, 3, effect)
            ).SetTarget(Scheme2)

            ability_y = AbilityFactory.ForChoiceAbility(
                "Heal 3 damage from an identity",
                lambda targets:
                    this.HealthUnits(targets, 3, effect)
            ).SetTarget(Identity, canbe_heal=True)

            ability_b =  AbilityFactory.ForChoiceAbility(
                "Deal 3 damage to an enemy",
                lambda targets:
                    this.DealDamage(targets, 3, effect)
            ).SetTarget(Enemy)

            if res.g:
                initiator.ChooseAbilities(
                    effect,
                    ability_r,
                    ability_y,
                    ability_b,
                )
            if res.r:
                initiator.ChooseAbilities(
                    effect,
                    ability_r
                )
            if res.y:
                initiator.ChooseAbilities(
                    effect,
                    ability_y
                )
            if res.b:
                initiator.ChooseAbilities(
                    effect,
                    ability_b
                )


    return [
        AbilityFactory.AfterUnitAttackUnit(
            AbilityType.Response,
            "This",
            None,
            adam_warlock
        ),
        AbilityFactory.AfterUnitThwartEnd(
            AbilityType.Response,
            "This",
            adam_warlock
        )
    ]

