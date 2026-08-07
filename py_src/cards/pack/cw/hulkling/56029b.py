from . import *

# * Teddy Altman

def GetAbilities() -> Sequence['Ability']:

    def teddy_altman(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            include_discard_pile=True,
            trait="SHAPESHIFT",
            card_type=Upgrade
        )
        if face:
            Faces.AddToHand([face], initiator, effect)


    return [
        AbilityFactory.PlayersCannotPlayCardWhile(
            "You",
            CardFinder2("SHAPESHIFT", Upgrade),
            conditions=[
                lambda effect, message:
                    effect.GetInitiator().GetControlCards2(CardFinder2("SHAPESHIFT", Upgrade)) != []
            ]
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            teddy_altman
        ).SetName("Shape-Changer")
        .LimitOncePerRound(),
    ]

