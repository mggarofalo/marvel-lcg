from . import *

# Wingman

def GetAbilities() -> Sequence['Ability']:

    def wingman(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.PreventDamage(1, effect)

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            CardFinder2("AERIAL", Ally)
        ),
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.Interrupt,
            ("Another", CardFinder(trait="AERIAL")),
            wingman,
            is_consequential_damage=True,
        ).SetCostFunc(CostFunc.Exhaust("Attached", card_type=Ally)),
    ]

