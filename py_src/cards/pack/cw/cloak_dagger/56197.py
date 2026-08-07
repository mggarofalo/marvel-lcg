from . import *

# Lightforce

def GetAbilities() -> Sequence['Ability']:

    def lightforce_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        this.HealthUnits("EnemyLeader", 3, effect)
        identity.TakeIndirectDamage(this, 3, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            lightforce_revealed
        ),
    ]

