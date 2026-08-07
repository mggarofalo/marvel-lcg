from . import *

# Assassination Attempt

def GetAbilities() -> Sequence['Ability']:

    def assassination_attempt_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        minions = Worlds.FindCardsOnField(
            effect,
            trait="ASSASSIN",
            card_type=Minion
        )

        if minions:
            for minion in minions:
                minion.DoAttackYou(player, effect)

        minions = Worlds.FindCardsOnField(
            effect,
            trait="ASSASSIN",
            card_type=Minion
        )
        if not minions:
            face = Search.EncounterCard(
                effect,
                player,
                include_discard_pile=True,
                trait="ASSASSIN",
                card_type=Minion,
            )
            if face:
                face.Reveal(player, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            assassination_attempt_revealed
        ),
    ]

