from . import *

# * Machine Man: Aaron Stack

def GetAbilities() -> Sequence['Ability']:

    def machine_man(effect: 'Effect', message: 'Message.WhenUnitWouldAttack|Message.WhenUnitWouldThwart') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        # A: Is Machine Man missing some text on his ability? And if not, how long do the stat boost last for?
        # Q: “Machine Man’s boost is only supposed to last for that activation. “

        value = effect.GetPaidResources().val
        this.GainForThisActive(
            effect,
            message,
            attack=value,
            thwart=value
        )


    return [
        AbilityFactory.WhenUnitWouldAttackOrThwart(
            AbilityType.Interrupt,
            "This",
            machine_man
        ).SetCost(Cost("3", up_to=True)),
    ]

