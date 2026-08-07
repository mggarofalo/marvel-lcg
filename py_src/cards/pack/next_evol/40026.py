from . import *

# Frenemies

def GetAbilities() -> Sequence['Ability']:

    def frenemies(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        this.DealDamage(effect.targets, 1, effect)

        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "",
                lambda targets:
                    this.RemoveThreatFromSchemes(targets, 3, effect)
            ).SetLabel('thwart')
            .SetTarget(Scheme2, range=(1, 2))
        )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            frenemies,
            conditions=[
                lambda effect, message:
                    effect.GetInitiator().GetIdentity().health > 1,
            ],
        ).SetPlay().SetLabel('thwart')
        .SetTarget("TeamUp")
        .SetTarget2(Scheme2),
    ]

