from . import *

# * Star-Lord's Helmet

def GetAbilities() -> Sequence['Ability']:

    def star_lords_helmet(effect: 'Effect', face: 'CardFace', ui: List['CardFace']) -> int:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        player = this.GetBindFace().GetControlByPlayer()

        if player.GetIdentity().card.state.is_flipping:
            return 0

        if player.IsHero():
            faces = player.dealt_encounter_cards.GetAll()
            ui.extend(faces)
            return min(3, len(faces))
        else:
            return 0

    return [
        *AbilityFactory.GiveKeywordToAttached(
            Hero,
            get_new_value=star_lords_helmet,
            hand_size=1,
            ex_change_on_event=OnEvent.DealtEncounterCard("You")
        )
    ]

