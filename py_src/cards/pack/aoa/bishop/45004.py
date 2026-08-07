from . import *

# * Bishop's Rifle

def GetAbilities() -> Sequence['Ability']:

    def bishops_rifle(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        size = len(initiator.hand_cards.FindCards(card_type=Resource))
        this.DealDamage(effect.targets, size, effect, property=AttackProperty(ranged=True))


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            bishops_rifle
        ).SetLabel('attack')
        .SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(Enemy),
    ]

