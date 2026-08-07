from . import *

# Targeted Strike

def GetAbilities() -> Sequence['Ability']:

    def targeted_strike_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        team = Worlds.GetEnemyTeam(effect)
        face = Search.EncounterCardTop(
            effect,
            team,
            5,
        )
        if face:
            player.DealEncounterCard(face, effect)

    def targeted_strike_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        value = Worlds.GetYourTeamSize(effect)
        this.GainBoostIcons(value, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            targeted_strike_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            targeted_strike_boost
        ),
    ]

