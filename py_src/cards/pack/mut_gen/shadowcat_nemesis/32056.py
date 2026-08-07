from . import *

# * White Queen

def GetAbilities() -> Sequence['Ability']:

    def white_queen_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()
        Faces.GiveStatus([identity], "Confused", effect)


    return [
        AbilityFactory.YouAreStateWhileThisInPlay(
            lambda effect: effect.this.CastTo(Minion).GetEngagedPlayer().GetIdentity(),
            "Confused"
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            white_queen_boost
        ),
    ]

