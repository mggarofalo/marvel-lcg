from . import *

# * Surprise Contender

def GetAbilities() -> Sequence['Ability']:

    def surprise_contender(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        champion = FindTheChampion(effect)
        if champion:
            Faces.PlaceCountersOn([champion], 1, 'ratings', effect)

    def surprise_contender_defeated(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        challengers = FindTheChallengers(effect)
        if challengers:
            Faces.PlaceCountersOn([challengers], "2*", 'ratings', effect)

    return [
        AbilityFactory.AfterUnitAttackAndDamageUnit(
            AbilityType.ForcedResponse,
            "This",
            None,
            surprise_contender,
        ),
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            surprise_contender_defeated
        ),
    ]

