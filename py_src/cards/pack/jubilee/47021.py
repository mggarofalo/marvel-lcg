from . import *

# Multitalented

def GetAbilities() -> Sequence['Ability']:

    def multitalented(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        res = effect.GetPaidResources(True)
        if res.HasColor("R"):
            initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        this.DealDamage(targets, 2, effect)
                ).SetLabel('attack')
                .SetTarget(Enemy)
            )
        if res.HasColor("B"):
            initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        this.RemoveThreatFromSchemes(targets, 2, effect)
                ).SetLabel('thwart')
                .SetTarget(Scheme2)
            )
        if res.HasColor("Y"):
            initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        this.HealthUnits(targets, 2, effect)
                ).SetTarget(
                    [initiator.GetIdentity()],
                    finder=CardFinder(canbe_heal=True))
            )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            multitalented
        ).SetPlay().SetLabel('attack', 'thwart'),
    ]

