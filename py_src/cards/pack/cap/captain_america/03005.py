from . import *

# Shield Block

def GetAbilities() -> Sequence['Ability']:

    def shield_block(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)
        message.PreventDamage("All", effect)

    return [
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.Interrupt,
            "You",
            shield_block,
        ).SetPlay().SetLabel('defense')
        .SetCostFunc(CostFunc.Exhaust(card_type=Upgrade, name=CAPTAIN_AMERICAS_SHIELD, from_where=["YouControlCards"]))
    ]
