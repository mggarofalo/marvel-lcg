from . import *

# * Magik's Crown

def GetAbilities() -> Sequence['Ability']:

    def magiks_crown(effect: 'Effect', attach: 'CardFace', ui: List['CardFace']) -> int:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        face = initiator.player_deck.GetTop()
        if face:
            res = FacesCounter.GetPrintedResources([face])
            if res.HasColor("B"):
                ui.append(face)
                return 1
        return 0


    return [
        *AbilityFactory.GiveKeywordToAttached(
            Hero,
            steady=1,
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Hero,
            get_new_value=magiks_crown,
            thwart=1,
            ex_change_on_event=OnEvent.DeckTopCard("YourDeck"),
        ),
    ]

