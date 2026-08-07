from . import *

# * Pinpoint: Qureshi Gupta

def GetAbilities() -> Sequence['Ability']:

    def pinpoint(effect: 'Effect', message: 'Message.WhenCardWouldMoveToArea') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        message.SetCannot(effect)

        deck = effect.targets[0].GetOwnerPlayer().player_deck
        Faces.ShuffleAllTo(effect.targets, deck, effect)


    return [
        AbilityFactory.CanPlayThisAllyCard(
        ).SetPlay(only_if_your_identity_has_trait="CHAMPION"),
        AbilityFactory.WhenCardWouldMoveToArea(
            AbilityType.HeroInterrupt,
            PlayerCard,
            pinpoint,
            from_play=True,
            into_discard_pile=True
        ).SetTarget("Trigger")
        .SetCostFunc(CostFunc.Exhaust("This")),
    ]

