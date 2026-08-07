from . import *

# Mutant Massacre

def GetAbilities() -> Sequence['Ability']:

    def mutant_massacre_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        from game.effect.rule import Advance

        this = effect.this.CastTo(MainScheme)
        Unused(this)

        def put_play_under_control(player: 'Player'):
            ally = Worlds.AsideDeck(effect).FindCard(name="Morlock", card_type=Ally)
            if ally:
                ally.PutIntoPlay(player, effect, under_control=True)

        def action(player: 'Player'):
            put_play_under_control(player)
        Players.ForEachPlayer(effect, action)

        if Worlds.GetPlayerNumIcon(effect) == 1:
            player = Worlds.GetFirstPlayer(effect)
            put_play_under_control(player)


        SetupCards.ShuffleIntoEncounterDeck(
            effect,
            name="Hide!",
            card_type=Treachery
        )

        if message.by_effect.this.IsName("Knock, Knock") and \
            not isinstance(message.by_effect, Advance) and \
            message.by_effect.ability.flags.is_response:
            faces = Worlds.FindCardsOnField(
                effect,
                name="Morlock",
                card_type=Ally
            )
            Faces.GiveStatus(faces, "Tough", effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            mutant_massacre_revealed
        ),
    ]

