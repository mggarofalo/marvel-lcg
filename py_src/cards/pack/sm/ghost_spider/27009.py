from . import *

# Web-Bracelet

def GetAbilities() -> Sequence['Ability']:

    def web_bracelet(effect: 'Effect', message: 'Message.AfterEffectResolved') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(1, effect)


    return [
        AbilityFactory.AfterPlayerResolveAbility(
            AbilityType.Response,
            "You",
            Event,
            web_bracelet,
            which_abilities=["Interrupt", "Response"],
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .LimitOncePerEvent()
    ]

