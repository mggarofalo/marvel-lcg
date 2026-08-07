from . import *

# Protect the Innocent

def GetAbilities() -> Sequence['Ability']:

    def protect_the_innocent_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        leader = Worlds.GetEnemyLeader(effect)
        enemies = Worlds.GetOnFieldMinions(effect)
        if leader:
            enemies += [leader]
        if leader and leader.IsTough():
            this.GainSurge(+1, effect)
        Faces.GiveStatus(enemies, "Tough", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            protect_the_innocent_revealed
        ),
    ]

