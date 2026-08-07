from . import *

# * Tusk

def GetAbilities() -> Sequence['Ability']:

    def tusk(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetAgainstPlayer()
        if player:
            identity = player.GetIdentity()
            Faces.GiveStatus([identity], "Stunned", effect)

    def tusk_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()
        Faces.GiveStatus([identity], "Stunned", effect)

    return [
        AbilityFactory.AfterUnitAttackYou(
            AbilityType.ForcedResponse,
            "This",
            tusk
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            tusk_boost
        ),
    ]

