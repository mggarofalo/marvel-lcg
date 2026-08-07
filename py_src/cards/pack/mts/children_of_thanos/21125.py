from . import *

# * Corvus Glaive

def GetAbilities() -> Sequence['Ability']:

    def corvus_glaive_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        player.DiscardControlCards(effect, ally=True, support=True)


    return [
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            corvus_glaive_boost
        ),
    ]

