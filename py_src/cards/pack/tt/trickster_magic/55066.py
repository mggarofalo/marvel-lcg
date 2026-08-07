from . import *

# * Zzzax

def GetAbilities() -> Sequence['Ability']:

    def zzzax(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()

        face = Search.PlayerDeckTop(
            effect,
            initiator,
            5,
            finder=CardFinder(has_printed_res="Y")
        )
        if face:
            Faces.AddToHand([face], initiator, effect)


    return [
        *AbilityFactory.ThisNotCountAllyLimit(),
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            zzzax
        ),
    ]

