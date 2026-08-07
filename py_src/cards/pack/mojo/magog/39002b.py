from . import *

# Melee in the Mojo-seum

def GetAbilities() -> Sequence['Ability']:

    def melee_in_the_mojo_seum(effect: 'Effect', message: 'Message.WhenMainSchemeStageWouldBeCompleted') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        message.SetBeInstead(effect)

        champion = FindTheChampion(effect)
        if champion:
            Faces.PlaceCountersOn([champion], "2*", 'ratings', effect)
            num = this.threat
            this.RemoveThreatFromSchemes([this], num, effect)

    return [
        # TODO: The players cannot win the game unless they wow the crowd as The Challengers.
        AbilityFactory.WhenMainSchemeStageWouldBeCompleted(
            AbilityType.WhenCompleted,
            "This",
            melee_in_the_mojo_seum
        ),
    ]

