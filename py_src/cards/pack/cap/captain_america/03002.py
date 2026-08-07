from . import *

# * Agent 13'

def GetAbilities() -> Sequence['Ability']:

    def agent_13(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 2, effect)

    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            agent_13
        )
        .SetTarget(Scheme2)
    ]
