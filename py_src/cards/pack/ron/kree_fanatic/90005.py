from . import *

# You Dare Oppose Me?

def GetAbilities() -> Sequence['Ability']:

    def you_dare_oppose_me_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        def action(face: 'CardFace'):
            if face.paper.set_name == "Kree Fanatic":
                player.DealEncounterCard(face, effect)

        Worlds.DiscardEncounterCards(5, effect, each_time=action)

    def you_dare_oppose_me_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        message.would_atk_message.GainOverKill(effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            you_dare_oppose_me_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            you_dare_oppose_me_boost,
            during_attack=True
        ),
    ]

