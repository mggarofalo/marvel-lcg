from . import *

# Diplomatic Sanctions

def GetAbilities() -> Sequence['Ability']:

    def diplomatic_sanctions_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        value = Worlds.VictoryDisplay(effect).FindCardSize(CardFinder2("A.I.M", Minion))

        player = message.GetToPlayer()
        player.DiscardHandCards((value, value), effect)

    def diplomatic_sanctions_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        value = Worlds.VictoryDisplay(effect).FindCardSize(CardFinder2("A.I.M", Minion))
        message.GainBoostIcon(value, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            diplomatic_sanctions_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            diplomatic_sanctions_boost
        ),
    ]

