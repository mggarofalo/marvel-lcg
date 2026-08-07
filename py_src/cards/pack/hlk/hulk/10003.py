from . import *

# Hulk Smash

def GetAbilities() -> Sequence['Ability']:

    def hulk_smash(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        message.GainATKForThisAttack(10, effect)

        if effect.GetPaidResources().OnlyHasColor("R"):
            message.GainOverKill(effect)

    return [
        AbilityFactory.WhenUnitMakeAttack(
            AbilityType.HeroInterrupt,
            "You",
            hulk_smash,
            is_basic_attack=True,
        ).SetPlay()
    ]

