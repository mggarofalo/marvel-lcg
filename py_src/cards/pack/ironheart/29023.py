from . import *

# * Snowguard: Amka Aliyak

def GetAbilities() -> Sequence['Ability']:

    def snowguard(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        num = initiator.DeclareNumber(1, 3)
        Faces.PlaceCountersOn([this], num, 'shift', effect)

    return [
        AbilityFactory.UnitAttackGainKeyword(
            "This",
            overkill=True,
            conditions=[
                lambda effect, message:
                    effect.this.CastTo(Ally).GetCounters('shift') == 1
            ],
        ),
        AbilityFactory.ThisGainKeyword(
            lambda effect, ui:
                effect.this.CastTo(Ally).GetCounters('shift') == 1,
            attack=3,
            change_on_event=OnEvent.Counter("This", 'shift'),
        ),
        AbilityFactory.ThisGainKeyword(
            lambda effect, ui:
                effect.this.CastTo(Ally).GetCounters('shift') == 2,
            thwart=3,
            trait="AERIAL",
            change_on_event=OnEvent.Counter("This", 'shift'),
        ),
        AbilityFactory.ThisGainKeyword(
            lambda effect, ui:
                effect.this.CastTo(Ally).GetCounters('shift') == 3,
            health=5,
            retaliate=1,
            change_on_event=OnEvent.Counter("This", 'shift'),
        ),
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            snowguard
        ),
    ]

