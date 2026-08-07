from . import *

# Web-Fluid Compressor

def GetAbilities() -> Sequence['Ability']:

    def web_fluid_compressor(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.GainATKForThisAttack(2, effect)


    return [
        AbilityFactory.WhenUnitMakeAttack(
            AbilityType.HeroInterrupt,
            CardFinder(name="SP//dr Suit"),
            web_fluid_compressor,
            is_basic_attack=True
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

