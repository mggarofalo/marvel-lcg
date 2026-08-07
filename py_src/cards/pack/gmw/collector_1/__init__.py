from cards.pack import *

def GetTheCollection(effect: 'Effect'):
    return Worlds.ScenarioDeck(effect, "TheCollection")

def PutTopCardIntoTheCollection(player: 'Player', by_effect: 'Effect'):
    face = player.player_deck.Get(True)[0]
    PutCardIntoTheCollection([face], by_effect)

# Face up only
def PutCardIntoTheCollection(faces: Sequence['CardFace'], by_effect: 'Effect'):
    Faces.FlipAllTo(faces, True, by_effect)
    Faces.MoveAllTo(faces, GetTheCollection(by_effect).deck, by_effect)

def PutCardIntoTheCollectionInsteadDiscard(place_threat: bool):
    def collector(effect: 'Effect', message: 'Message.WhenCardWouldMoveToArea') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        message.ChangeToArea(GetTheCollection(effect).deck, effect)

        if place_threat:
            scheme = Worlds.FindMainScheme(this)
            if scheme:
                this.PlaceThreatOnSchemes([scheme], 1, effect)

    return AbilityFactory.WhenCardWouldMoveToArea(
        AbilityType.ForcedInterrupt,
        None,
        collector,
        conditions=[
            lambda effect, message:
                (
                    PlayerCard.IsType(message.trigger) or \
                    EncounterCard.IsType(message.trigger)
                ) and \
                message.into_area.flags.is_discards and \
                not message.into_area.flags.is_out_of_play and \
                message.from_area.flags.is_in_play
        ],
    )

