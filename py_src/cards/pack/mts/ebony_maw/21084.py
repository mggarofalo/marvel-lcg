from . import *

# Reactor Overload

def GetAbilities() -> Sequence['Ability']:

    def reactor_overload_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        def action(player: 'Player'):
            player.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "Take 2 damage",
                    lambda targets:
                        targets[0].CastTo(Unit2).TakeDamage(this, 2, effect)
                ).SetTarget("YourIdentity"),
                AbilityFactory.ForChoiceAbility(
                    "Place 2 threat here",
                    lambda targets:
                        this.PlaceThreatOnSchemes(targets, 2, effect)
                ).SetTarget("This")
            )
        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            reactor_overload_revealed
        ),
    ]

