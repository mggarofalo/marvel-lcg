from . import *

# * U.S. Agent

def GetAbilities() -> Sequence['Ability']:

    def us_agent(effect: 'Effect', message: 'Message.WhenCardRevealed|Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        this.DealDamage([identity], 1, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            us_agent
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            us_agent
        ),
    ]

