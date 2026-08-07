from . import *

# * The Circe

def GetAbilities() -> Sequence['Ability']:

    def the_circe(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        def action2(player: 'Player'):
            def action(targets: Sequence['CardFace']):
                for face in targets:
                    face.PutIntoPlay(player, effect)

            player.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    action
                ).SetTarget(Ally, from_where=["YourHandCards"])
            )

        Players.ForEachTargets(effect.targets, action2)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            the_circe
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Counter("This", 1, 'deploy'))
        .SetTarget("Players"),
    ]

