from . import *

# * Ant-Man's Helmet

def GetAbilities() -> Sequence['Ability']:

    def ant_mans_helmet_heal(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.HealthUnits(effect.targets, 2, effect)

    def ant_mans_helmet_draw(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        for target in effect.targets:
            player = target.GetControlByPlayer()
            player.DrawUp(1, effect)


    return [
        AbilityFactory.AfterUnitChangeForm(
            AbilityType.HeroResponse,
            "You",
            ant_mans_helmet_heal,
            to_form=CardFinder2("GIANT", Hero),
        ).SetTarget("YourHero", canbe_heal=True),
        AbilityFactory.AfterUnitChangeForm(
            AbilityType.HeroResponse,
            "You",
            ant_mans_helmet_draw,
            to_form=CardFinder2("TINY", Hero),
        ).SetTarget("Initiator")
    ]

