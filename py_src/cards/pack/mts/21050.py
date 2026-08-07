from . import *

# Zone of Silence

def GetAbilities() -> Sequence['Ability']:

    def zone_of_silence(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        faces = effect.cost_func.Get(CostFunc.DiscardDeckTopCards).return_discarded_cards
        value = len(faces)

        this.RemoveThreatFromSchemes(effect.targets, value, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            zone_of_silence
        ).SetPlay(only_if_your_identity_has_trait="MYSTIC").SetLabel('thwart')
        .SetTarget(Scheme2)
        .SetCostFunc(CostFunc.DiscardDeckTopCards("YourDeck", (1, 4))),
    ]

