from . import *

# Sidekick

def GetAbilities() -> Sequence['Ability']:

    def sidekick(effect: 'Effect', message: 'Message.AfterUnitRecovery') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.HealthUnits(effect.targets, 2, effect)


    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            Select.From("YourAlly", finder=CardFinder(card_class="IdentitySpecific"))
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Ally,
            health=2,
        ),
        AbilityFactory.AfterUnitMakeRecovery(
            AbilityType.Response,
            "You",
            sidekick,
            is_basic_recovery=True
        ).SetTarget("Attached"),
    ]

