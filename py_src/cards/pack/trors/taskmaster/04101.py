from . import *

# Hydra Hunter

def GetAbilities() -> Sequence['Ability']:

    def hydra_hunter_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        if player.IsInHeroForm():
            player.GetHero().TakeDamage(this, 1, effect)
        else:
            scheme = Worlds.FindMainScheme(this)
            if scheme:
                this.PlaceThreatOnSchemes([scheme], 1, effect)


    return [
        AbilityFactory.UnitAttackGainKeyword(
            "This",
            piercing=True,
            ranged=True
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            hydra_hunter_boost
        ),
    ]

