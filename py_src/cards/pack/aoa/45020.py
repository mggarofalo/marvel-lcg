from . import *

# * Legion: David Haller

def GetAbilities() -> Sequence['Ability']:

    def legion(effect: 'Effect', message: 'Message.AfterUnitUseBasicPower') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        face = initiator.DiscardDeckTopCard(effect)
        res = FacesCounter.GetPrintedResources([face])
        if res.y:
            initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        this.DealDamage(targets, 2, effect)
                ).SetTarget(Enemy)
            )
        if res.b:
            initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        this.RemoveThreatFromSchemes(targets, 2, effect)
                ).SetTarget(Scheme2)
            )
        if res.r:
            initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        this.HealthUnits(targets, 2, effect)
                ).SetTarget(name="Legion", canbe_heal=True)
            )


    return [
        AbilityFactory.AfterUnitUseBasicPower(
            AbilityType.Response,
            "This",
            legion
        ),
    ]

