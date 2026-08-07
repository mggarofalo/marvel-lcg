from . import *

# * Deathcry: Sharra Neramani

def GetAbilities() -> Sequence['Ability']:

    def deathcry(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.GainForThisActive(
            effect,
            message,
            attack_consequential_damage=-1
        )


    return [
        AbilityFactory.WhenUnitMakeAttack(
            AbilityType.HeroInterrupt,
            "This",
            deathcry,
            is_basic_attack=True
        ).SetCostFunc(CostFunc.DealDamage(1, "YourHero")),
    ]

