from . import *

# Aerial Evacuation

def GetAbilities() -> Sequence['Ability']:

    def aerial_evacuation(effect: 'Effect', message: 'Message.WhenFaceWouldDealDamage') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()

        message.PreventDamage("All", effect)
        initiator.GetIdentity().ChangeToForm(AlterEgo, effect)

        if Hero.IsType(message.target):
            message.target.ChangeToForm(AlterEgo, effect)

    return [
        AbilityFactory.WhenFaceWouldDealDamage(
            AbilityType.HeroInterrupt,
            None,
            aerial_evacuation,
            who_take_damage="AnotherFriend",
            # who_take_damage=CardFinder(
            #     card_type=Friend,
            #     check_effect_fn=lambda effect, face:
            #         face != effect.GetInitiator().GetIdentity()
            # )
        ).SetCostFunc(CostFunc.Discard("This")),
    ]

