from . import *

# Covert Ops

def GetAbilities() -> Sequence['Ability']:

    def covert_ops_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()
        Faces.GiveStatus([identity], "Confused", effect)

        villain = Worlds.FindVillain(effect, name="Black Widow")
        if villain:
            villain.DoSchemes(player, effect)

    def covert_ops_preparation(effect: 'Effect', message: 'Message.WhenResolvePreparationAbility') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        this.PlaceThreatOnSchemes("EachScheme", 1, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            covert_ops_revealed
        ),
        AbilityFactory.WhenResolvePreparationAbility(
            "This",
            covert_ops_preparation
        ),
    ]

