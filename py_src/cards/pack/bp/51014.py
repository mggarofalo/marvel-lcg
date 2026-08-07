from . import *

# * Manifold: Eden Fesi

def GetAbilities() -> Sequence['Ability']:

    def manifold(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        player = effect.targets[0].GetControlByPlayer()
        face = Search.PlayerCard(
            effect,
            player,
            include_player_deck=True,
            include_discard_pile=True,
            card_type=PlayerSideScheme,
        )
        if face:
            Faces.AddToHand([face], player, effect)


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            manifold
        ).SetTarget("Players"),
    ]

