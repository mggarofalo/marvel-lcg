from . import *

# Magic Attack

def GetAbilities() -> Sequence['Ability']:

    def magic_attack(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        faces = effect.cost_func.Get(CostFunc.DiscardDeckTopCards).return_discarded_cards
        damage = len(faces)
        this.DealDamage(effect.targets, damage, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            magic_attack
        ).SetPlay(only_if_your_identity_has_trait="MYSTIC").SetLabel('attack')
        .SetTarget(Enemy)
        .SetCostFunc(CostFunc.DiscardDeckTopCards("YourDeck", (1, 5))),
    ]

