from . import *

# Teleport Drop

def GetAbilities() -> Sequence['Ability']:

    def teleport_drop(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 8, effect)
        Faces.GiveStatus(effect.targets, "Stunned", effect)

    def teleport_drop_cost(targets: Sequence['CardFace'], effect: 'Effect') -> bool:
        # For stunned
        if not effect.targets:
            return False
        bamf = effect.targets[0].GetInventoryDeck().FindCard(name="Bamf!")
        if not bamf:
            return False
        return not not Faces.DiscardAll([bamf], effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            teleport_drop
        ).SetPlay().SetLabel('attack')
        .SetCostFunc(CostFunc.Custom(None, teleport_drop_cost))
        .SetTarget(Enemy, with_attach=CardFinder(name="Bamf!")),
    ]

