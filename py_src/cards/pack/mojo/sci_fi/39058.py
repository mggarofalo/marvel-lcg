from . import *

# Toad 2.0

def GetAbilities() -> Sequence['Ability']:

    def toad_20(effect: 'Effect', message: 'Message.AfterMinionEngagePlayer') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.engaged_player
        faces = player.GetControlUpgrade()
        face = player.AskChooseFace(faces, effect)
        if face:
            this.PlaceCardHere(face, False, effect)


    def toad_20_defeated(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        faces = this.GetPlacedCardArea().Get()
        Faces.ReturnToHand(faces, "Owner", effect)


    return [
        AbilityFactory.AfterMinionEngagePlayer(
            AbilityType.ForcedResponse,
            "This",
            "You",
            toad_20
        ),
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            toad_20_defeated
        ),
    ]

