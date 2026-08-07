from . import *

# Mob Mentality

def GetAbilities() -> Sequence['Ability']:

    def mob_mentality_reveal_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        faces = Worlds.DiscardEncounterCards(7, effect)
        for face in faces:
            if Minion.IsType(face):
                face.PutIntoPlay(player, effect)
                break


    def mob_mentality_reveal_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        Worlds.Enemies.VillainAndEngagedMinionsAttackYou(effect, player)

        ThisCardGainSurge(effect)


    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            mob_mentality_reveal_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            mob_mentality_reveal_hero
        ),
    ]

