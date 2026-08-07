from . import *

# Not My Lucky Day

def GetAbilities() -> Sequence['Ability']:

    def not_my_lucky_day_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        def action(player: 'Player'):
            player.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "Take 1 damage",
                    lambda targets:
                        player.GetIdentity().TakeDamage(this, 1, effect)
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
            not_my_lucky_day_revealed
        ),
    ]

