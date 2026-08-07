from . import *

# * Mystical Armor

def GetAbilities() -> Sequence['Ability']:

    def mystical_armor(effect: 'Effect', attach: 'CardFace', ui: List['CardFace']) -> int:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        face = initiator.player_deck.GetTop()
        if face:
            res = FacesCounter.GetPrintedResources([face])
            if res.HasColor("Y"):
                ui.append(face)
                return 1
        return 0


    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Magik"),
            retaliate=1,
        ),
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Magik"),
            get_new_value=mystical_armor,
            defense=1,
            ex_change_on_event=OnEvent.DeckTopCard("YourDeck"),
        ),
    ]

