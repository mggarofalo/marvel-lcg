from . import *

# Domino's Pistol

def GetAbilities() -> Sequence['Ability']:

    def dominos_pistol(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        faces = effect.cost_func.Get(CostFunc.DiscardDeckTopCards).return_discarded_cards
        value = FacesCounter.GetPrintedResourcesIcon(faces, initiator.player_deck)

        this.DealDamage(effect.targets, value, effect, property=AttackProperty(ranged=True))


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            dominos_pistol
        ).SetLabel('attack')
        .SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.DiscardDeckTopCards("YourDeck", 1))
        .SetTarget(Enemy),
    ]

