from . import *

# "I Am Inevitable"

def GetAbilities() -> Sequence['Ability']:

    def i_am_inevitable_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        villain = Worlds.FindCardOnField(effect, name="Thanos")
        if villain:
            Faces.GiveFacedownBoostCards([villain], 1, effect)

    def i_am_inevitable_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
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
            i_am_inevitable_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            i_am_inevitable_boost
        ),
    ]

