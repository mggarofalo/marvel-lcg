from . import *

# * Psylocke: Betsy Braddock

def GetAbilities() -> Sequence['Ability']:

    def psylocke(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.HealthUnits(effect.targets, 1, effect)

    def psylocke_archangel(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)

    return [
        AbilityFactory.AfterUnitAttackEnd(
            AbilityType.HeroResponse,
            "This",
            psylocke,
            conditions=[
                lambda effect, message:
                    effect.GetInitiator().GetIdentity().IsName("Angel")
            ]
        ).SetTarget("This", canbe_heal=True),
        AbilityFactory.AfterUnitAttackEnd(
            AbilityType.HeroResponse,
            "This",
            psylocke_archangel,
            conditions=[
                lambda effect, message:
                    effect.GetInitiator().GetIdentity().IsName("Archangel")
            ]
        ).SetTarget("YourHero", canbe_ready=True),
    ]

