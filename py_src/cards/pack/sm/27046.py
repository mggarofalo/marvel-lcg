from . import *

# * Agent 13: Sharon Carter

def GetAbilities() -> Sequence['Ability']:

    def agent_13(effect: 'Effect', message: 'Message.AfterUnitAttackEnd|Message.AfterUnitThwartEnd') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.AfterUnitAttackOrThwart(
            AbilityType.Response,
            "This",
            agent_13
        ).SetTarget(Support, trait="S.H.I.E.L.D", canbe_ready=True),
    ]

