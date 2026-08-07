from . import *

# * Lady Mastermind

def GetAbilities() -> Sequence['Ability']:

    def lady_mastermind_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        faces = player.hand_cards.FindCards(card_type=Event)
        faces = Filter.All(faces, HasCost, highest_cost=True, max_size=1)
        value = FacesCounter.GetPrintedCost(faces)
        identity.TakeDamage(this, value, effect)

    def lady_mastermind_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        faces = player.hand_cards.FindCards(card_type=Event)
        player.AskDiscardFace(faces, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            lady_mastermind_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            lady_mastermind_boost
        ),
    ]

