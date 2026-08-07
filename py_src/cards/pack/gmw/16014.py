from . import *

# Fighting Fit

def GetAbilities() -> Sequence['Ability']:

    def fighting_fit(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        hero = initiator.GetHero()
        damage = 2
        if hero.health >= hero.starting_health:
            damage = 5
        this.DealDamage(effect.targets, damage, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            fighting_fit
        ).SetPlay().SetLabel('attack')
        .SetTarget(Villain),
    ]

