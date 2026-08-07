from . import *

# Bounty Hunting

def GetAbilities() -> Sequence['Ability']:

    def bounty_hunting_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        if identity.HasTrait("UNREGISTERED"):
            faces = player.DiscardRandomHandCards(1, effect)
        else:
            faces = player.DiscardHandCards((1, 1), effect)
        if faces:
            face = faces[0]
            if HasResourceIcon.IsType(face):
                this.PlaceThreatOnSchemes("MainScheme", face.printed_resource.val, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            bounty_hunting_revealed
        ),
    ]

