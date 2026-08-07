from . import *

# * Outlaw: Inez Temple

def GetAbilities() -> Sequence['Ability']:

    def outlaw(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        faces = effect.cost_func.Get(CostFunc.DiscardDeckTopCards).return_discarded_cards
        value = FacesCounter.GetPrintedResourcesIcon(faces, initiator.player_deck)
        message.GainATKForThisAttack(value, effect)

    return [
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.Interrupt,
            "This",
            outlaw
        ).SetCostFunc(CostFunc.DiscardDeckTopCards("YourDeck", 1)),
    ]

