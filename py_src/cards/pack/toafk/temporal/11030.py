from . import *

# Ancient Warrior

def GetAbilities() -> Sequence['Ability']:

    def ancient_warrior(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        Faces.GiveStatus([player.GetIdentity()], "Stunned", effect)

    return [
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            ancient_warrior
        )
    ]

