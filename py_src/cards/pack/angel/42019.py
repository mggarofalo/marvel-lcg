from . import *

# Containment Strategy

def GetAbilities() -> Sequence['Ability']:

    def containment_strategy(effect: 'Effect', message: 'Message.AfterUnitDefendEnd') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        value = 1
        if message.dealt_damage == 0:
            value = 2
        this.RemoveThreatFromSchemes(effect.targets, value, effect)


    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            CardFinder(card_type=SchemeSide2, is_permanent=False)
        ),
        AbilityFactory.AfterUnitDefendAgainstAttack(
            AbilityType.Response,
            Hero,
            containment_strategy
        ).SetTarget("Attached"),
    ]

