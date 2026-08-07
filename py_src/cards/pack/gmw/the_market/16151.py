from . import *

# By Any Means

def GetAbilities() -> Sequence['Ability']:

    def by_any_means(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        this.PlaceThreatOnSchemes("MainScheme", 2, effect)
        this.DealDamage(effect.targets, 3, effect)
        initiator.DrawUp(1, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            by_any_means
        ).SetPlay().SetLabel('attack')
        .SetTarget(Villain)
        .SetHasNoTargetEffect(),
    ]

