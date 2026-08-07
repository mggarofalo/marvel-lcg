from . import *

# "Threat or Menace?"

def GetAbilities() -> Sequence['Ability']:

    def threat_or_menace_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        YouMayChangeForm(player, effect)

        if player.IsInHeroForm():
            scheme = Worlds.FindMainScheme(this)
            if scheme:
                this.PlaceThreatOnSchemes([scheme], 2, effect)
        else:
            Players.CannotChangeFormDuringNextTurn(player, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            threat_or_menace_revealed
        ),
    ]

