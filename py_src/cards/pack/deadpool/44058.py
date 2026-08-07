from . import *

# * War

def GetAbilities() -> Sequence['Ability']:

    def war(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        hero = initiator.GetHero()

        face = Worlds.DiscardEncounterTopCard(effect)
        if face:
            value = 0
            value += FacesCounter.CountTotalBoostIcons([face])
            value += FacesCounter.CountTotalBoostStar([face])
            hero.TakeDamage(face, value, effect)

        face = initiator.DiscardDeckTopCard(effect)
        damage = FacesCounter.GetTotalCost([face])
        this.DealDamage(effect.targets, damage, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            war
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(Enemy),
    ]

