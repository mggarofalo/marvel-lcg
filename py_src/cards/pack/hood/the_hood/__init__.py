from cards.pack import *

def FoulPlay(num: int, only_first: bool) -> 'Ability':
    def foul_play(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility'):
        faces = Worlds.DiscardEncounterCards(num, effect)
        player = message.GetToPlayer()
        if player:
            for face in faces:
                if not face.paper.IsFromSet("The Hood"):
                    player.DealEncounterCard(face, effect)
                    if only_first:
                        break

    return AbilityFactory.WhenResolveSpecialAbility(
        "This",
        foul_play
    ).SetName("Foul Play")

def EachPlayerMustResolveFoulPlayAbility(by_effect: 'Effect'):
    this = by_effect.this
    Unused(this)

    def action(player: 'Player'):
        ResolveFoulPlayerAbility(player, by_effect)
    Players.ForEachPlayer(by_effect, action)

def ResolveFoulPlayerAbility(player: 'Player', by_effect: 'Effect'):
    this = by_effect.this
    Unused(this)

    villain = Worlds.FindVillain(by_effect)
    if villain:
        villain.ResolveSpecialAbility(player, by_effect, name="Foul Play")

