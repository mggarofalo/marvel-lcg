from . import *

# Infinite Mischief

def GetAbilities() -> Sequence['Ability']:

    def infinite_mischief_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        GetInfinityStone(effect).deck.ShuffleWithDiscardPile(False, effect)
        face = GetInfinityStone(effect).GetTopCard()
        if face:
            face.Reveal(None, effect)

    def infinite_mischief_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        face = GetInfinityStone(effect).GetTopCard()
        if face:
            Faces.DiscardAll([face], effect)
            icon = FacesCounter.CountTotalBoostIcons([face])
            message.GainBoostIcon(icon, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            infinite_mischief_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            infinite_mischief_boost
        ),
    ]

