from . import *

# Bring Them In

def GetAbilities() -> Sequence['Ability']:

    def bring_them_in_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        faces = Worlds.GetPlayersIdentities(effect, CardFinder(trait="UNREGISTERED"))
        if not Faces.ExhaustAll(faces, effect):
            team = Worlds.GetEnemyTeam(effect)
            face = Search.EncounterCard(
                effect,
                team,
                include_discard_pile=True,
                name="Unregistered Super",
                card_type=Obligation
            )
            if face:
                player.DealEncounterCard(face, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            bring_them_in_revealed
        ),
    ]

