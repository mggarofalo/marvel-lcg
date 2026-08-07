from . import *

# Sentinel Mark II

def GetAbilities() -> Sequence['Ability']:

    def sentinel_mark_ii_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        scheme = Worlds.FindCardOnField(
            effect,
            name="Operation Zero Tolerance"
        )
        if scheme:
            ThisCardGainSurge(effect)
        else:
            scheme = Search.EncounterCard(
                effect,
                include_discard_pile=True,
                name="Operation Zero Tolerance",
                card_type=SchemeSide2
            )
            if scheme:
                scheme.Reveal(None, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            sentinel_mark_ii_revealed
        ),
    ]

