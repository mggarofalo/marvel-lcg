from . import *

# Museum Ship

def GetAbilities() -> Sequence['Ability']:

    def museum_ship(effect: 'Effect', message: 'Message.WhenPhaseBegin') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        first_player = Worlds.GetFirstPlayer(effect)
        player_num = Worlds.GetPlayerNumIcon(effect)

        def assign_damage(value: int):
            units = [x for y in Worlds.GetPlayers(effect) for x in y.GetControlCharacters()]
            first_player.AssignDamage(units, this, value, effect)

        first_player.ChooseAbilities(
            effect,
            ExhaustTheMilanoThenForChoiceAbility(
                f"Exhaust the Milano → assign {2 * player_num} indirect damage among players",
                lambda targets:
                    assign_damage(2 * player_num)
            ),
            AbilityFactory.ForChoiceAbility(
                f"Assign {3 * player_num} indirect damage among players",
                lambda targets:
                    assign_damage(3 * player_num)
            )
        )


    return [
        AbilityFactory.WhenVillainPhaseBegin(
            AbilityType.ForcedInterrupt,
            museum_ship
        ).SetName('"Hold on to your butts!"')
    ]

