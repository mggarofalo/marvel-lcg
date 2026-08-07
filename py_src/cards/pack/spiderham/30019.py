from . import *

# Overwatch

def GetAbilities() -> Sequence['Ability']:

    def overwatch(effect: 'Effect', message: 'Message.AfterSchemeRemoveThreat') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        value = message.value
        this.RemoveThreatFromSchemes(effect.targets, value, effect)

    return [
        AbilityFactory.CanPlayThisUpgradeCard(Scheme2),
        AbilityFactory.AfterSchemeRemoveThreat(
            AbilityType.HeroInterrupt,
            "AttachedScheme",
            overwatch,
            by_thwart=True,
        ).SetCostFunc(CostFunc.Discard("This"))
        .SetTarget(Scheme2,
            check_fn=lambda effect, face: face != effect.this.GetBindFace())
    ]

