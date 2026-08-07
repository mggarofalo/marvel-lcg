from . import *

# Shapeshifter Surprise

def GetAbilities() -> Sequence['Ability']:

    def shapeshifter_surprise_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        Faces.ShuffleAllTo([this], player.player_deck, effect)
        ThisCardGainSurge(effect)

    def shapeshifter_surprise(effect: 'Effect', message: 'Message.AfterCardEnterHand') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        mystique = Worlds.FindCardOnField(
            effect,
            name="Mystique",
            card_type=Minion
        )
        if mystique:
            mystique.DoAttackYou(player, effect)
        else:
            mystique = Search.EncounterCard(
                effect,
                include_discard_pile=True,
                name="Mystique",
                card_type=Minion,
            )
            if mystique:
                mystique.Reveal(None, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            shapeshifter_surprise_revealed
        ),
        AbilityFactory.AfterCardEnterHand(
            AbilityType.ForcedResponse,
            "This",
            shapeshifter_surprise
        ),
    ]

