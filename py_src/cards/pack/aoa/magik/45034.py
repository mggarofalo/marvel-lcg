from . import *

# * Soulsword

def GetAbilities() -> Sequence['Ability']:

    def soulsword(effect: 'Effect', attach: 'CardFace', ui: List['CardFace']) -> int:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        face = initiator.player_deck.GetTop()
        if face:
            res = FacesCounter.GetPrintedResources([face])
            if res.HasColor("R"):
                ui.append(face)
                return 1
        return 0

    return [
        AbilityFactory.UnitAttackGainKeyword(
            CardFinder(name="Magik"),
            is_basic_attack=True,
            piercing=True,
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Hero,
            get_new_value=soulsword,
            attack=1,
            ex_change_on_event=OnEvent.DeckTopCard("YourDeck"),
        ),
    ]

