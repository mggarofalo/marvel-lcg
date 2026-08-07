from . import *

# Passion for Justice

def GetAbilities() -> Sequence['Ability']:

    def passion_for_justice(effect: 'Effect', message: 'Message.WhenCardBeSpendAsResource') -> None:
        this = effect.this.CastTo(Resource)
        Unused(this)

        this.effect.RegisterTemp(
            AbilityFactory.WhenUnitWouldThwart(
                AbilityType.Temp0,
                "You",
                lambda effect, thw_message:
                    thw_message.RemoveAdditionalThreat(1, effect),
                conditions=[
                    lambda effect, thw_message:
                        message.for_effect == thw_message.by_effect,
                ]
            ),
            unregister_after_exec=True,
            until_resolve_effect=message.for_effect
        )

    return [
        AbilityFactory.WhenYouSpendThisCardToPlay(
            AbilityType.HeroResponse,
            passion_for_justice,
            to_pay_card=CardFinder2("THWART", Event)
        ),
    ]

