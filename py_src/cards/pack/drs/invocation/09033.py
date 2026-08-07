from . import *

# Images of Ikonn

def GetAbilities() -> Sequence['Ability']:

    def images_of_ikonn_thwart(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Confused", effect)
        this.RemoveThreatFromSchemes(effect.targets2, 4, effect)

        PlaceThisCardInInvocationDeckDiscardPile(this, effect)

    return [
        # Q: Can I resolve Images of Ikonn if the villain is already
        # confused and there is no threat on a scheme?
        # A: Yes. As long as any part of the ability on Images of Ikonn
        # can change the game state, then it will do as many effects
        # as it is able. While its first effect may fail to change the
        # game state if the villain is already confused and there is no
        # threat on a scheme, its second effect can change the game
        # state by placing itself in the Invocation deck discard pile.
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            images_of_ikonn_thwart,
        ).SetTarget(Villain)
        .SetTarget2(Scheme2)
        .SetHasNoTargetEffect()
        .NoOutOfPlayLimit()
        .LimitCardNameOncePerEvent(),
    ]

