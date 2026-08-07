from . import *

# Cell Phone

def GetAbilities() -> Sequence['Ability']:

    def cell_phone(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        def action2(player: 'Player'):
            player.MakeBasicPowerLikeInTurn(
                player.GetControlCharacters(),
                effect,
                ["ATK", "THW"],
                when_use=lambda face, message:
                    face.GainForThisActive(
                        effect,
                        message.would_message,
                        thwart=1,
                        attack=1,
                    )
            )

        Players.ForEachTargets(effect.targets, action2)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            cell_phone
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Counter("This", 1, 'charge'))
        .SetTarget("Players"),
    ]

