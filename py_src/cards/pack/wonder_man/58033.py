from . import *

# Disarming Defense

def GetAbilities() -> Sequence['Ability']:

    def disarming_defense(effect: 'Effect', message: 'Message.WhenUnitWouldDefend') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        message.GainDEFForThisAttack(+2, effect)


    return [
        AbilityFactory.WhenUnitDefendAgainstAttack(
            AbilityType.Interrupt,
            "YourHero",
            disarming_defense,
        ).SetPlay().SetLabel('defense'),
    ]

