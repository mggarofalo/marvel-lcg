from . import *

# * Wolfsbane: Rahne Sinclair

def GetAbilities() -> Sequence['Ability']:

    def wolfsbane(effect: 'Effect', message: 'Message.AfterUnitThwartEnd') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        card_type = initiator.DeclareCardType()
        face = initiator.DiscardDeckTopCard(effect)
        if card_type.IsType(face):
            initiator.MayChooseOneAbility(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        initiator.GainCard(targets, effect)
                ).SetTarget([face])
            )

    return [
        AbilityFactory.AfterUnitThwartEnd(
            AbilityType.Response,
            "This",
            wolfsbane
        ),
    ]

