from . import *

# * Brother Voodoo: Jericho Drumm

def GetAbilities() -> Sequence['Ability']:

    def brother_voodoo(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.PlayerDeckTop(
            effect,
            initiator,
            5,
            card_type=Event
        )

        if face:
            Faces.AddToHand([face], initiator, effect)


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            brother_voodoo
        ),
    ]

