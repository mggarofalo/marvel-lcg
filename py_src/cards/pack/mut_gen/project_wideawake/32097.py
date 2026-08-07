from . import *

# Self-Repair

def GetAbilities() -> Sequence['Ability']:

    def self_repair_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        villain = Worlds.FindVillain(effect)
        if villain:
            villain.DiscardEachStatus(effect)

            Faces.GiveStatus([villain], "Tough", effect)
            this.HealthUnits([villain], 5, effect)

    def self_repair_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        villain = Worlds.FindVillain(effect)
        if villain:
            Faces.GiveStatus([villain], "Tough", effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            self_repair_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            self_repair_boost
        ),
    ]

