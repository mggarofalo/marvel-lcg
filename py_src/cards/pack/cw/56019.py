from . import *

# * Yellow Jacket: Hank Pym

def GetAbilities() -> Sequence['Ability']:

    def yellow_jacket(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.PlayerDeckTop(
            effect,
            initiator,
            5,
            card_type=Upgrade
        )

        if face:
            Faces.AddToHand([face], initiator, effect)


    return [
        AbilityFactory.CanPlayThisAllyCard(
        ).SetPlay(only_if_your_identity_has_trait="AVENGER"),
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            yellow_jacket
        ),
    ]

