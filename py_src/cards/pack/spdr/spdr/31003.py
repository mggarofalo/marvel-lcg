from . import *

# * VEN#m: Addy Brock

def GetAbilities() -> Sequence['Ability']:

    def venm(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        value = GetResourceGeneratedBySyncRatio(effect)
        Faces.PlaceCountersOn([this], value, 'sym', effect)


    return [
        AbilityFactory.ThisGainKeyword(
            lambda effect, ui:
                effect.this.CastTo(Ally).GetCounters('sym'),
            attack=1,
            thwart=1,
            change_on_event=OnEvent.Counter("This", 'sym')
        ),
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.HeroResponse,
            "This",
            venm
        ),
    ]

