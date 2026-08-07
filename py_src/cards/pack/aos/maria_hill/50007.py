from . import *

# Special Funding

def GetAbilities() -> Sequence['Ability']:

    def special_funding(effect: 'Effect', message: 'Message.AfterCardsBeSpendAsResource') -> None:
        this = effect.this.CastTo(Resource)
        Unused(this)

        def action(face: 'CardFace'):
            Faces.PlaceCountersOn([face], 1, 'all-purpose', effect)

        this.effect.RegisterTemp(
            AbilityFactory.AfterCardEnterPlay(
                AbilityType.Temp0,
                message.pay_for_card,
                lambda effect, message:
                    action(message.trigger)
            ),
            unregister_after_exec=True,
            until_turn_end=True
        )

    return [
        AbilityFactory.AfterYouSpendThisCard(
            AbilityType.Response,
            special_funding,
            to_pay_card=CardFinder2("S.H.I.E.L.D", Support)
        ),
    ]

