from . import *

# Calculate the Odds

def GetAbilities() -> Sequence['Ability']:

    def calculate_the_odds(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(1, effect)

        def action(targets: Sequence['CardFace']):
            for target in targets:
                player = target.GetControlByPlayer()
                def action2():
                    player.DrawUp(1, effect)
                    player.DiscardHandCards((1, 1), effect)

                player.MayChooseOneAbility(
                    effect,
                    AbilityFactory.ForChoiceAbility(
                        "",
                        lambda targets:
                            action2()
                    )
                )

        action(effect.targets2)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            calculate_the_odds
        ).SetPlay().SetLabel()
        .SetTarget2("Players"),
    ]

