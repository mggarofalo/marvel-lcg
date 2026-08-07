from . import *

# Summoning Spell

def GetAbilities() -> Sequence['Ability']:

    def summoning_spell(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        face = effect.cost_func.Get(CostFunc.DiscardDeckTopUntil).return_discard_card
        face.PutIntoPlay(initiator, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            summoning_spell
        ).SetPlay(only_if_your_identity_has_trait="MYSTIC").SetLabel()
        .SetCostFunc(CostFunc.DiscardDeckTopUntil("YourDeck", Ally)),
    ]

