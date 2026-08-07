from . import *

# Technovirus Resurgence

def GetAbilities() -> Sequence['Ability']:

    def technovirus_resurgence(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        face = Search.SearchForCard(
            effect,
            player,
            include_player_deck=True,
            include_player_discard_pile=True,
            include_player_hand_cards=True,
            include_victory_display=True,
            name="Technovirus Purge",
            card_type=SchemeSide2,
        )
        if face:
            face.PutIntoPlay(player, effect)

        attched = False

        face = Worlds.FindCardOnField(
            effect,
            name="Technovirus Purge"
        )
        if face:
            attched = this.AttachTo2(face, effect)

        if not attched:
            Faces.DiscardAll([this], effect)
            player.DealEncounterCards(1, effect)

    return [
        AbilityFactory.WhenThisObligationRevealed(
            technovirus_resurgence
        )
    ]

