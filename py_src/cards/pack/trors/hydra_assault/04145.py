from . import *

# Hydra Flame-Soldier

def GetAbilities() -> Sequence['Ability']:

    def hydra_flame_soldier(effect: 'Effect', message: 'Message.AfterUnitAttackUnit|Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        player.DiscardControlCards(effect, support=True)


    return [
        AbilityFactory.AfterUnitAttackUnit(
            AbilityType.ForcedResponse,
            "This",
            "You",
            hydra_flame_soldier,
            is_undefended_attack=True,
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            hydra_flame_soldier,
            is_undefended_attack=True,
        ),
    ]

