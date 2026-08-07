from . import *

# Concussive Blast

def GetAbilities() -> Sequence['Ability']:

    def concussive_blast_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        units = Worlds.GetOnFieldFriendlyCharacters(effect)
        this.DealDamage(units, 1, effect)

    def concussive_blast_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        player = message.GetToPlayer()
        units = player.GetControlCharacters()
        this.DealDamage(units, 1, effect)

    return [
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            concussive_blast_boost
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            concussive_blast_revealed
        )
    ]
