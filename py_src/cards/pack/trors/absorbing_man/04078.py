from . import *

# * Absorbing Man

def GetAbilities() -> Sequence['Ability']:

    def absorbing_man(effect: 'Effect', message: 'Message.AfterEnemyActivationEnd') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        player = message.GetAgainstPlayer()
        if this.HasTrait('ICE') or this.HasTrait('STONE'):
            scheme = Worlds.FindMainScheme(this)
            if scheme:
                this.PlaceThreatOnSchemes([scheme], 1, effect)
        if this.HasTrait('METAL') or this.HasTrait('WOOD'):
            player.GetIdentity().TakeIndirectDamage(this, 1, effect)


    return [
        *AbsorbingManGainsTraitOfEnvironment(),
        AbilityFactory.AfterEnemyActivatesAgainstYou(
            AbilityType.ForcedResponse,
            "This",
            absorbing_man,
        ),
    ]

