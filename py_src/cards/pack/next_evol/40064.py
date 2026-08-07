from . import *

# Sharpshooter

def GetAbilities() -> Sequence['Ability']:

    def sharpshooter(effect: 'Effect', message: 'Message.WhenUnitMakeKeyWordAttack') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        faces = effect.cost_func.Get(CostFunc.DiscardDeckTopCards).return_discarded_cards
        damage = FacesCounter.GetPrintedResourcesIcon(faces, initiator.player_deck)
        message.DealAdditionalDamage(damage, effect)


    return [
        AbilityFactory.WhenUnitMakeKeyWordAttack(
            AbilityType.HeroInterrupt,
            "You",
            sharpshooter,
            has_keyword="Ranged"
        ).SetCostFunc(CostFunc.DiscardDeckTopCards("YourDeck", 1)),
    ]

