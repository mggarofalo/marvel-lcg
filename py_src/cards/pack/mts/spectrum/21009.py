from . import *

# Pulsar Shield

def GetAbilities() -> Sequence['Ability']:

    def pulsar_shield(effect: 'Effect', message: 'Message.WhenUnitWouldDefend') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        hero = initiator.GetIdentity()
        do_gain = initiator.form.IsInEnergyForm("Pulsar")

        initiator.form.ChangeEnergyForm(effect, "Pulsar")
        Faces.ReadyAll([hero], effect)

        if do_gain:
            hero.GainUntilPhaseEnd(effect, retaliate=1)


    return [
        AbilityFactory.WhenUnitDefendAgainstAttack(
            AbilityType.HeroInterrupt,
            "You",
            pulsar_shield
        ).SetPlay().SetLabel('defense'),
    ]

