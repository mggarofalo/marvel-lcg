from . import *

# Effective Leadership

def GetAbilities() -> Sequence['Ability']:

    def effective_leadership(effect: 'Effect', message: 'Message.WhenCardBeSpendAsResource') -> None:
        this = effect.this.CastTo(Resource)
        Unused(this)

        unit = message.pay_for_card
        if unit:
            def action():
                unit.GainUntilPhaseEnd(
                    effect,
                    thwart=1,
                    attack=1
                )
            RunAt.AfterFaceEnterPlay(effect, unit, action)

    return [
        AbilityFactory.WhenYouSpendThisCardToPlay(
            AbilityType.Interrupt,
            effective_leadership,
            CardFinder(card_type=Ally),
        ),
    ]

