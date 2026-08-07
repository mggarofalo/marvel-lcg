from . import *

# * Beak: Barnell Bohusk

def GetAbilities() -> Sequence['Ability']:

    def beak(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        allies = initiator.GetControlAllies()
        value = Faces.FindCardSize(allies, CardFinder2("X-MEN"))
        this.RemoveThreatFromSchemes(effect.targets, value, effect)


    return [
        AbilityFactory.AfterYouPlayThisFromHand(
            AbilityType.Response,
            beak,
        ).SetTarget(Scheme2),
    ]

