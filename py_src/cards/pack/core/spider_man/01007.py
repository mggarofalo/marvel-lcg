from . import *

# Spider-Tracer

def GetAbilities() -> Sequence['Ability']:

    def spider_tracer(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 3, effect)

    return [
        AbilityFactory.CanPlayThisUpgradeCard(Minion),
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.ForcedInterrupt,
            "AttachedMinion",
            spider_tracer
        ).SetTarget(Scheme2)
    ]

