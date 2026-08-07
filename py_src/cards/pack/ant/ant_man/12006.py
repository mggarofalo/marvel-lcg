from . import *

# Pym Particles

def GetAbilities() -> Sequence['Ability']:

    def pym_particles_giant(effect: 'Effect', message: 'Message.AfterCardsBeSpendAsResource') -> None:
        this = effect.this.CastTo(Resource)
        Unused(this)

        this.HealthUnits(effect.targets, 2, effect)


    def pym_particles_tiny(effect: 'Effect', message: 'Message.AfterCardsBeSpendAsResource') -> None:
        this = effect.this.CastTo(Resource)
        Unused(this)

        for target in effect.targets:
            player = target.GetControlByPlayer()
            player.DrawUp(1, effect)


    return [
        AbilityFactory.AfterYouSpendThisCard(
            AbilityType.HeroResponse,
            pym_particles_giant,
            is_in_hero_from="GIANT",
        ).SetTarget("YourHero", canbe_heal=True),
        AbilityFactory.AfterYouSpendThisCard(
            AbilityType.HeroResponse,
            pym_particles_tiny,
            is_in_hero_from="TINY",
        ).SetTarget("Initiator"),
    ]

