from . import *

# Ready for a Fight

def GetAbilities() -> Sequence['Ability']:

    def ready_for_a_fight(effect: 'Effect', message: 'Message.WhenUnitWouldScheme') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        effect.targets[0].CastTo(Identity).ChangeToForm(Hero, effect)
        message.SetBeInstead(effect)

        message.trigger.CastTo(Enemy).DoAttackYou(initiator, effect)


    return [
        AbilityFactory.WhenUnitWouldScheme(
            AbilityType.Interrupt,
            Enemy,
            ready_for_a_fight
        ).SetCostFunc(CostFunc.Discard("This"))
        .SetTarget("YourIdentity"),
    ]

