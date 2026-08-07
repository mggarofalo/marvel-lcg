from cards.pack import *

def Mojo(discard_size: int) -> 'Ability':
    def mojo(effect: 'Effect', message: 'Message.AfterPlayerTurnEnd') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()
        faces = Worlds.DiscardEncounterCards(discard_size, effect)
        num = len(faces) - Faces.FindCardSize(faces, CardFinder(set_name="Mojo"))

        Faces.PlaceTokensOn([identity], num, 'threat', effect)


    return AbilityFactory.AfterPlayerTurnEnd(
            AbilityType.ForcedResponse,
            PlayerFinder(card_type=Hero),
            mojo
        )

