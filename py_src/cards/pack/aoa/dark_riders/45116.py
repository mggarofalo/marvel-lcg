from . import *

# * Psynapse

def GetAbilities() -> Sequence['Ability']:

    def psynapse(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetAgainstPlayer()
        if player:
            identity = player.GetIdentity()
            Faces.GiveStatus([identity], "Confused", effect)

    def psynapse_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()
        Faces.GiveStatus([identity], "Confused", effect)

    return [
        AbilityFactory.AfterUnitAttackYou(
            AbilityType.ForcedResponse,
            "This",
            psynapse
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            psynapse_boost
        ),
    ]

