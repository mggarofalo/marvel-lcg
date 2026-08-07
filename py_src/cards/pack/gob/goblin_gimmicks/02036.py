from . import *

# Regenerative Healing

def GetAbilities() -> Sequence['Ability']:

    def regenerative_healing(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        villain = Worlds.FindVillain(effect)

        if villain:
            if not this.HealthUnits([villain], villain.printed_stage*2, effect):
                this.GainSurge(1, effect)

    def regenerative_healing_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        villain = Worlds.FindVillain(effect)
        if villain:
            this.HealthUnits([villain], 2, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            regenerative_healing
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            regenerative_healing_boost
        )
    ]

