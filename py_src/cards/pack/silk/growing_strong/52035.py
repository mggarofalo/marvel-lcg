from . import *

# * Atlas

def GetAbilities() -> Sequence['Ability']:

    def atlas(effect: 'Effect', message: 'Message.AfterPhaseEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        Faces.PlaceCountersOn([this], 1, 'growth', effect)


    return [
        AbilityFactory.ThisGainKeyword(
            lambda effect, ui:
                effect.this.CastTo(Minion).GetCounters('growth'),
            health=2,
            change_on_event=OnEvent.Counter("This", 'growth')
        ),
        AbilityFactory.AfterPhaseEnd(
            AbilityType.ForcedResponse,
            "Villain",
            atlas
        ),
    ]

