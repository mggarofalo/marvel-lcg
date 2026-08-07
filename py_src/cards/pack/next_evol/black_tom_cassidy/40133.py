from . import *

# Creeping Willow

def GetAbilities() -> Sequence['Ability']:

    def creeping_willow(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        Faces.GiveStatus(message.attacked_targets, "Stunned", effect)

    def creeping_willow_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        Faces.GiveStatus([identity], "Stunned", effect)


    return [
        AbilityFactory.AfterUnitAttackAndDamageUnit(
            AbilityType.ForcedResponse,
            "This",
            None,
            creeping_willow,
            target_in_play=True
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            creeping_willow_boost
        ),
    ]

