from . import *

# Repair Sequence

def GetAbilities() -> Sequence['Ability']:

    def repair_sequence(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)

        player = message.GetToPlayer()

        health = 0
        for unit in player.GetEngagedMinions():
            if unit.HasTrait("DRONE"):
                health += 2

        villain = Worlds.FindVillain(effect)
        if villain:
            if health == 0 or not this.HealthUnits([villain], health, effect):
                this.GainSurge(1, effect)

    def repair_sequence_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        health = 0
        for unit in player.GetEngagedMinions():
            if unit.HasTrait("DRONE"):
                health += 1
        villain = Worlds.FindVillain(effect)
        if villain:
            this.HealthUnits([villain], health, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            repair_sequence
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            repair_sequence_boost
        )
    ]
