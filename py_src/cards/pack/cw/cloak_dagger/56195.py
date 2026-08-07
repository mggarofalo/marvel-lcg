from . import *

# * Dagger

def GetAbilities() -> Sequence['Ability']:

    def dagger_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        this.HealthUnits("EnemyLeader", 2, effect)
        Utility.DealDamageToCharacterYouControl(player, 2, effect)

    def dagger_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        if Worlds.FindCardOnField(
            effect,
            name="Cloak",
        ):
            this.Reveal(player, effect)
        else:
            Utility.DealDamageToCharacterYouControl(player, 1, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            dagger_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            dagger_boost
        ),
    ]

