from . import *

# Lucky and Good

def GetAbilities() -> Sequence['Ability']:

    def lucky_and_good(effect: 'Effect', message: 'Message.WhenBoostCardTurnedFaceUp') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.CancelAllBoostIcons(effect)
        message.CancelBoostAbility(effect)

        face = message.would_atk_message.attacker
        Faces.GiveFacedownBoostCards([face], 1, effect)


    return [
        AbilityFactory.WhenBoostCardTurnedFaceUp(
            AbilityType.HeroInterrupt,
            None,
            lucky_and_good,
            while_attack=True,
            activate_target="YourIdentity"
        ).SetLabel('defense')
        .SetCostFunc(CostFunc.Exhaust("This")),
    ]

