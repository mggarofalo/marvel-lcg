from . import *

# Gang-Up

def GetAbilities() -> Sequence['Ability']:

    def gang_up_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        Worlds.Enemies.VillainAndEngagedMinionsAttackYou(effect, player)

    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            ThisCardGainSurge
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            gang_up_hero
        ),
    ]
