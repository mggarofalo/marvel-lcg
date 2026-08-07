from . import *

# Rapid Response

def GetAbilities() -> Sequence['Ability']:

    def rapid_response(effect: 'Effect', message: 'Message.AfterUnitBeDefeated') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        message.trigger.PutIntoPlay(initiator, effect)
        this.DealDamage([message.trigger], 1, effect)


    return [
        AbilityFactory.AfterUnitBeDefeated(
            AbilityType.HeroResponse,
            "YouControlAlly",
            rapid_response,
            conditions=[
                lambda effect, message:
                    message.trigger.card.area.flags.is_discards
            ],
        ).SetCostFunc(CostFunc.Discard("This"))
        .SetTarget("Trigger", from_where=["YourDiscardPile"]),
    ]

