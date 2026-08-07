from . import *

# * M: Monet St. Croix

def GetAbilities() -> Sequence['Ability']:

    def m(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.DefeatUnits(effect.targets, this, effect)


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            m
        ).SetTarget(Minion, check_fn=lambda effect, face:
            effect.this.CastTo(Ally).health > face.CastTo(Minion).health),
    ]

