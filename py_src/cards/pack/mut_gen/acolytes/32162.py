from . import *

# * Delgado

def GetAbilities() -> Sequence['Ability']:

    def delgado(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        villain = Worlds.FindVillain(effect)
        if villain:
            villain.DiscardStunned(effect, rule="All")
            villain.DiscardConfused(effect, rule="All")
            Faces.GiveFacedownBoostCards([villain], 1, effect)

    return [
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            delgado
        ),
    ]

