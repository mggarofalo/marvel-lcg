from . import *
from cards.pack.trors.campaign import *

# The Island of Dr. Zola - 1A

def GetAbilities() -> Sequence['Ability']:

    def the_island_of_dr_zola(effect: 'Effect', message: 'Message.WhenCardSetup') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        SetupCards.Reveal(
            effect,
            name="Hydra Prison",
            card_type=SchemeSide2
        )

        def action(player: 'Player'):
            face = Search.EncounterCard(
                effect,
                player,
                include_discard_pile=False,
                name="Ultimate Bio-Servant",
                card_type=Minion,
            )
            if face:
                face.PutIntoPlay(player, effect)

        Players.ForEachPlayer(effect, action)

    return [
        *CampaignSetup(4),
        AbilityFactory.WhenCardSetup(
            "This",
            the_island_of_dr_zola
        ),
    ]

