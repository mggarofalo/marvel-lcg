from . import *

# Mutant Terrorists

def GetAbilities() -> Sequence['Ability']:

    def mutant_terrorists_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        face = Search.EncounterCard(
            effect,
            include_discard_pile=True,
            name="The Brotherhood",
            card_type=SchemeSide2,
        )

        def action():
            face = Worlds.DiscardEncounterCardsUntil(
                effect,
                card_type=Minion,
                trait="BROTHERHOOD OF MUTANTS",
            )
            if face:
                face.Reveal(None, effect)

        if face:
            face.Reveal(None, effect, if_no_entered_play=action)
        else:
            action()

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            mutant_terrorists_revealed
        ),
    ]

