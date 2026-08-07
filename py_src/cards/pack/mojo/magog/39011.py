from . import *

# Stage Fright

def GetAbilities() -> Sequence['Ability']:

    def stage_fright_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()
        Faces.GiveStatus([identity], "Confused", effect)
        num = 2
        champion = FindTheChampion(effect)
        challengers = FindTheChallengers(effect)
        if champion and challengers:
            if challengers.GetCounters('ratings') >= champion.GetCounters('ratings'):
                num = 4
        scheme = Worlds.FindMainScheme(this)
        if scheme:
            this.PlaceThreatOnSchemes([scheme], num, effect)

    def stage_fright_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()
        Faces.GiveStatus([identity], "Confused", effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            stage_fright_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            stage_fright_boost
        ),
    ]

