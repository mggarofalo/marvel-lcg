from . import *

# Village Under Attack

def GetAbilities() -> Sequence['Ability']:

    def village_under_attack(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        def action(player: 'Player'):
            ResolveSpecialAbilityOnTheSETTINGEnvironment(player, effect)

        Players.ForEachPlayer(effect, action)

    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            village_under_attack,
        ),
    ]

