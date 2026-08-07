from . import *

# Winter, Widow, Soldier, Spy

def GetAbilities() -> Sequence['Ability']:

    def winter_widow_soldier_spy(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        for face in effect.targets2:
            face.PutIntoPlay(initiator, effect)

        this.DealDamage(effect.targets3, 4, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            winter_widow_soldier_spy
        ).SetPlay().SetLabel('attack')
        .SetTarget("TeamUp")
        .SetTarget2(trait="PREPARATION", card_type=Upgrade, from_where=["YourDiscardPile"])
        .SetTarget3(Enemy),
    ]

