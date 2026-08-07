from . import *

# Magic Muscle

def GetAbilities() -> Sequence['Ability']:

    def magic_muscle_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        enemies = Worlds.FindCardsOnField(
            effect,
            trait="BRUTE",
            card_type=Enemy
        )
        if not Faces.GiveStatus(enemies, "Tough", effect):
            face = Worlds.DiscardEncounterCardsUntil(
                effect,
                card_type=Minion,
                trait="BRUTE",
            )
            if face:
                face.Reveal(player, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            magic_muscle_revealed
        ),
    ]

