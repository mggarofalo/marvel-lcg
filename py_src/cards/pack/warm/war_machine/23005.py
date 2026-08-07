from . import *

# Gauntlet Gun

def GetAbilities() -> Sequence['Ability']:

    def gauntlet_gun(effect: 'Effect', message: 'Message.WhenPlayerPayingResources') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        identity = initiator.GetIdentity()
        Faces.PlaceCountersOn([identity], 1, 'ammo', effect)

    return [
        AbilityFactory.CanGenerateResources(
            AbilityType.Resource,
            Resources("G"),
            for_card=CardFinder(card_class="IdentitySpecific", card_type=Event),
            ex_operation=gauntlet_gun
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

