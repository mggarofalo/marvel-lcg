from . import *

# * Jessica Jones

def GetAbilities() -> Sequence['Ability']:

    def jessica_jones(effect: 'Effect', message: 'Message.WhenCardRevealed|Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        if not Faces.GiveStatus("YourLeader", "Confused", effect):
            Faces.GiveStatus([identity], "Confused", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            jessica_jones
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            jessica_jones
        ),
    ]

