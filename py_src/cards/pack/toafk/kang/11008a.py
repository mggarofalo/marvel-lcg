from . import *

# The Master of Time - 2A

def GetAbilities() -> Sequence['Ability']:

    def the_master_of_time(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        side_schemes = Worlds.GetSideSchemes(effect)
        this.PlaceAccelerationToken(len(side_schemes), effect)
        Faces.DiscardAll(side_schemes, effect)

        schemes = Worlds.MainSchemesDeck(effect).FindCards(
            card_ids=["11009a", "11010a", "11011a", "11012a"],
            card_type=MainScheme,
        )

        def action(player: 'Player'):
            scheme = Rand.RandomChoice(schemes, effect)
            schemes.remove(scheme)
            scheme.Reveal(player, effect)

        Players.ForEachPlayer(effect, action)

        Faces.RemoveAllFromGame(schemes, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            the_master_of_time
        )
    ]

