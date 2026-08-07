from . import *

# * Furnax

def GetAbilities() -> Sequence['Ability']:

    def furnax(effect: 'Effect', message: 'Message.AfterEnemyActivationEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        def action(player: 'Player'):
            this.DealIndirectDamage(player.GetIdentity(), 2, effect)

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.AfterEnemyActivationEnd(
            AbilityType.ForcedResponse,
            "This",
            furnax
        ),
    ]

