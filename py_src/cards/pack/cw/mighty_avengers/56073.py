from . import *

# * Yellow Jacket

def GetAbilities() -> Sequence['Ability']:

    def yellow_jacket(effect: 'Effect', message: 'Message.WhenCardRevealed|Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        leader = Worlds.GetEnemyLeader(effect)
        if leader:
            Faces.GiveStatus([leader], "Tough", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            yellow_jacket
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            yellow_jacket
        ),
    ]

