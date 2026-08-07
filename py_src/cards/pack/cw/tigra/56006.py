from . import *

# Cat-Like Reflexes

def GetAbilities() -> Sequence['Ability']:

    def cat_like_reflexes(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.PreventDamage(3, effect)

        initiator = effect.GetInitiator()
        initiator.MayChooseOneAbility(
            effect,
            AbilityFactory.ForChoiceAbility(
                "",
                lambda targets:
                    Faces.GiveStatus(targets, "Confused", effect)
            ).SetTarget(Minion, canbe_confused=True)
        )


    return [
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.HeroInterrupt,
            "You",
            cat_like_reflexes
        ).SetCostFunc(CostFunc.Discard("This")),
    ]

