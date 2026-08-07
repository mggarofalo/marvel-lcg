from . import *

# Avian Anatomy

def GetAbilities() -> Sequence['Ability']:

    def avian_anatomy(effect: 'Effect', message: 'Message.AfterCardsBeSpendAsResource') -> None:
        this = effect.this.CastTo(Resource)
        Unused(this)

        initiator = effect.GetInitiator()
        def action():
            # TODO: Bug after gain 28012, it cannot be use again in this event
            if message.pay_for_card:
                Faces.ReturnToHand([message.pay_for_card], initiator, effect)

        RunAt.AfterResolveEffect(effect, message.for_effect, action)


    return [
        AbilityFactory.AfterYouSpendThisCard(
            AbilityType.Response,
            avian_anatomy,
            to_pay_card=CardFinder2("AERIAL", Event),
        ),
    ]

