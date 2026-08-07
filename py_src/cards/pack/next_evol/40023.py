from . import *

# * Mission Leader

def GetAbilities() -> Sequence['Ability']:

    def mission_leader(effect: 'Effect', message: 'Message.AfterSchemeBeDefeated') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        def action(player: 'Player'):
            player.DrawUp(1, effect)
        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.ReduceCostToPlayThis(
            1,
            your_identity_has_traits=["SOLDIER"],
        ),
        AbilityFactory.AfterSchemeBeDefeated(
            AbilityType.HeroResponse,
            SchemeSide2,
            mission_leader
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("This"),
    ]

