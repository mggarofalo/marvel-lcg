from . import *

# Quick Shift

def GetAbilities() -> Sequence['Ability']:

    def quick_shift(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        if initiator.form.IsInMassForm("Solid"):
            initiator.form.ChangeMassForm(effect, "Phased")
        elif initiator.form.IsInMassForm("Phased"):
            initiator.DrawUp(2, effect)

    return [
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.HeroInterrupt,
            Enemy,
            quick_shift
        ).SetPlay().SetLabel('defense'),
    ]

