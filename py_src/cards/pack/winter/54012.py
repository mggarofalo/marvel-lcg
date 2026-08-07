from . import *

# * Captain America: Steve Rogers

def GetAbilities() -> Sequence['Ability']:

    def captain_america(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            captain_america
        ).SetTarget(trait="S.H.I.E.L.D", card_type=Unit2, canbe_ready=True),
    ]

