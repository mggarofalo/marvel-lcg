from . import *

# Portal Through Time

def GetAbilities() -> Sequence['Ability']:


    def portal_through_time(effect: 'Effect', message: 'Message.WhenPlayerRevealCard') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        if CanSurge.IsType(message.trigger):
            message.trigger.GainSurge(+1, effect)

    return [
        AbilityFactory.WhenPlayerRevealCard(
            AbilityType.ForcedInterrupt,
            "AnyPlayer",
            CardFinder2("TEMPORAL"),
            None,
            portal_through_time,
        ).LimitOncePerPhase(),
    ]

