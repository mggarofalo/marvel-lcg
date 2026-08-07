from . import *

# * Maria Hill

def GetAbilities() -> Sequence['Ability']:

    def maria_hill_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        team = Worlds.GetEnemyTeam(effect)
        face = Search.EncounterCard(
            effect,
            team,
            include_discard_pile=True,
            name="Life Model Decoy",
            card_type=Attachment
        )
        if face:
            face.AttachTo2(this, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            maria_hill_revealed
        ),
    ]

