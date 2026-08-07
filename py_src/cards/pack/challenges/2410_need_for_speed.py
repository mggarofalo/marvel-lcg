from . import *

# Need for Speed

def GetAbilities() -> Sequence['Ability']:

    def need_for_speed(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Challenge)

        def check_3_times(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
            Worlds.SetGameOver(True, effect)

        def check_2_times(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
            this.effect.RegisterTemp(
                AbilityFactory.AfterUnitBeAttacked(
                    AbilityType.Temp0,
                    CardFinder(name="Speed Demon", card_type=Minion),
                    check_3_times,
                    attacker=CardFinder(name="Quicksilver", card_type=Hero),
                ),
                unregister_after_exec=True,
                until_turn_end=True
            )

        this.effect.RegisterTemp(
            AbilityFactory.AfterUnitBeAttacked(
                AbilityType.Temp0,
                CardFinder(name="Speed Demon", card_type=Minion),
                check_2_times,
                attacker=CardFinder(name="Quicksilver", card_type=Hero),
            ),
            unregister_after_exec=True,
            until_turn_end=True
        )

    def check_game_over(effect: 'Effect', message: 'Message.WhenGameOver'):
        if message.IsRule():
            return False

    return [
        AbilityFactory.AfterUnitBeAttacked(
            AbilityType.Challenge,
            CardFinder(name="Speed Demon", card_type=Minion),
            need_for_speed,
            attacker=CardFinder(name="Quicksilver", card_type=Hero),
        ),
        AbilityFactory.WhenScenarioEnd(
            check_game_over
        ),
    ]

