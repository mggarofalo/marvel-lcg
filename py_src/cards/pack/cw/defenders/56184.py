from . import *

# * Luke Cage

def GetAbilities() -> Sequence['Ability']:

    def luke_cage(effect: 'Effect', message: 'Message.WhenCardRevealed|Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        if not Faces.GiveStatus("YourLeader", "Stunned", effect):
            Faces.GiveStatus([identity], "Stunned", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            luke_cage
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            luke_cage
        ),
    ]

