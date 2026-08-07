from . import *

# Honor Among Thieves

def GetAbilities() -> Sequence['Ability']:

    def honor_among_thieves_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        face = Worlds.DiscardEncounterCardsUntil(
            effect,
            card_type=Minion,
            trait="CRIMINAL",
        )
        if face:
            face.Reveal(player, effect)
            Faces.GiveStatus([face], "Tough", effect)
            villain = Worlds.FindVillain(effect)
            if villain:
                Faces.GiveFacedownBoostCards([villain], 1, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            honor_among_thieves_revealed
        ),
    ]

