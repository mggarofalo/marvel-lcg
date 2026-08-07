from . import *

# * Ruckus

def GetAbilities() -> Sequence['Ability']:

    def ruckus_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        faces = player.GetControlCharacters()
        Faces.GiveStatus(faces, "Stunned", effect)

    def ruckus_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        Faces.GiveStatus([identity], "Stunned", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            ruckus_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            ruckus_boost
        ),
    ]

