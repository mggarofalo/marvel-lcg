from . import *

# * Spider-Man

def GetAbilities() -> Sequence['Ability']:

    def spider_man_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        faces = Worlds.GetOnFieldCharacters(effect, CardFinder(with_attach=CardFinder(name="Tangled Up")))
        def action():
            team = Worlds.GetEnemyTeam(effect)
            face = Search.EncounterCard(
                effect,
                team,
                include_discard_pile=True,
                name="Tangled Up"
            )
            if face:
                player.DealEncounterCard(face, effect)
        this.BasicAttack(faces, effect, if_no_attack_was_made=action)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            spider_man_revealed
        ),
    ]

