from . import *

# * Goliath

def GetAbilities() -> Sequence['Ability']:

    def goliath(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        def discard_this() -> None:
            Faces.DiscardAll([this], effect)

        this.GainAttack(4, effect)

        RunAt.TheEndOfThePhase(effect, discard_this)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            goliath
        ).LimitOncePerPhase()
        # .SetDesc('Goliath gets +4 ATK until the end of the phase')
    ]

