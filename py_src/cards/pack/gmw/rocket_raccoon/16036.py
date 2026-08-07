from . import *

# Particle Cannon

def GetAbilities() -> Sequence['Ability']:

    def particle_cannon(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        hero = initiator.GetHero()
        hero.DealDamage(effect.targets, 4, effect, property=AttackProperty(overkill=True, ranged=True))


    return [
        AbilityFactory.ThisEnterPlayWithCounters(2, 'charge'),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            particle_cannon
        ).SetLabel('attack')
        .SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Counter("This", 1, 'charge'))
        .SetTarget(Enemy),
    ]

