from . import *

# * Iron Man

def GetAbilities() -> Sequence['Ability']:

    def iron_man_revealed(effect: 'Effect', message: 'Message.WhenCardSetup') -> None:
        this = effect.this.CastTo(Leader)
        Unused(this)

        team = Worlds.GetEnemyTeam(effect)
        face = Search.EncounterCard(
            effect,
            team,
            include_discard_pile=False,
            card_type=Attachment,
            set_name="Iron Man"
        )
        if face:
            player = Worlds.GetYourTeam(effect)
            face.Reveal(player, effect)

    return [
        IronManGetsSCHForEachTechAttachmentOnHim(),
        AbilityFactory.WhenCardSetup(
            "This",
            iron_man_revealed
        ),
    ]

