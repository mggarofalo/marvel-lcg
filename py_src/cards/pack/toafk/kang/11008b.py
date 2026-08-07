from . import *

# The Master of Time - 2B

def GetAbilities() -> Sequence['Ability']:

    def the_master_of_time(effect: 'Effect', message: 'Message.WhenCardWouldBePlacedToken') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        message.SetBeInstead(effect)
        this.PlaceAccelerationToken(message.num, effect)

    return [
        AbilityFactory.WhenCardWouldBePlacedToken(
            AbilityType.ForcedInterrupt,
            "AnotherScheme",
            'acceleration_token',
            the_master_of_time,
        ).NoGameAreaLimit(),
        AbilityFactory.WhenMainSchemeWouldAdvance(
            AbilityType.NonKeyword,
            MainScheme,
            lambda effect, message:
                message.SetBeInstead(effect),
            conditions=[
                lambda effect, message:
                    effect.this != message.trigger,
            ]
        ).NoGameAreaLimit(),
        AbilityFactory.WhenSchemeWouldPlaceThreat(
            AbilityType.NonKeyword,
            "This",
            lambda effect, message:
                message.SetBeInstead(effect)
        ),
        AbilityFactory.WhenSchemeWouldPlaceThreat(
            AbilityType.NonKeyword,
            None,
            lambda effect, message:
                message.UpdateValue(effect.this.CastTo(MainScheme).acceleration_token, effect),
            conditions=[
                lambda effect, message:
                    effect.this != message.trigger and \
                    type(message.by_effect) is MainSchemePhasePlaceThreat,
            ]
        ).NoGameAreaLimit()
    ]

