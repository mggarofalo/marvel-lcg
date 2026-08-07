from . import *

# Inconspicuous Box

def GetAbilities() -> Sequence['Ability']:

    def inconspicuous_box_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        faces = player.GetControlCards()
        faces = Filter.All(faces, lowest_cost=True)
        face = player.AskChooseFace(faces, effect)
        if face:
            PutCardIntoTheCollection([face], effect)
        else:
            ThisCardGainSurge(effect)

    def inconspicuous_box_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        PutTopCardIntoTheCollection(player, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            inconspicuous_box_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            inconspicuous_box_boost,
            conditions=[
                lambda effect, message:
                    GetTheCollection(effect).deck.GetSize() <= 3 * Worlds.GetPlayerNumIcon(effect)
            ],
        ),
    ]

