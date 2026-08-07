from . import *

# Frostbite

def GetAbilities() -> Sequence['Ability']:

    def frostbite(effect: 'Effect', message: 'Message.AfterEnemyActivationEnd|Message.AfterCardLeavePlay') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.SetAside([this], effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            Enemy,
            scheme=-1,
            attack=-1,
        ),
        AbilityFactory.AfterEnemyActivationEnd(
            AbilityType.ForcedResponse,
            "AttachedEnemy",
            frostbite
        ),
        AbilityFactory.AfterCardLeavePlay(
            AbilityType.ForcedResponse,
            "AttachedEnemy",
            frostbite
        ),
    ]

