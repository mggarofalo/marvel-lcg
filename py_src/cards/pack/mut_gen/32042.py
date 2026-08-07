from . import *

# * Magik: Illyana Rasputin

def GetAbilities() -> Sequence['Ability']:

    def magik(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.ShuffleAllTo(effect.targets, "EncounterDeck", effect)


    return [
        AbilityFactory.AfterYouPlayThisFromHand(
            AbilityType.Response,
            magik,
        ).SetCost(Cost("B"))
        .SetTarget(Minion, non_trait="ELITE", engaged_with=CardFinder2("X-MEN", Hero)),
    ]

