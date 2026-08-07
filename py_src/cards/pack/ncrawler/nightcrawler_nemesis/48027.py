from . import *

# * Azazel

def GetAbilities() -> Sequence['Ability']:

    def azazel_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = Worlds.FindPlayerByName("Kurt Wagner", effect)
        if player:
            player.DealEncounterCard(this, effect)

    return [
        *AbilityFactory.UnitCannotHaveUpgradeAttached(
            "This"
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            azazel_boost
        ),
    ]

