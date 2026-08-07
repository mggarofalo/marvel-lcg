from . import *

# * Bad Boy

def GetAbilities() -> Sequence['Ability']:

    def bad_boy(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()

        message.PreventDamage("All", effect)
        initiator.GetIdentity().ChangeToForm(AlterEgo, effect)
        initiator.DrawUp(2, effect)


    return [
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.HeroInterrupt,
            "You",
            bad_boy,
            is_from_attack=True,
            who_deal_damage=Villain,
        ).SetCostFunc(CostFunc.Discard("This"))
    ]

