from . import *

# * Edison's Giant Robot

def GetAbilities() -> Sequence['Ability']:

    def edisons_giant_robot(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        this.TreatAsIfBlank(effect, until_phase_end=True)


    return [
        AbilityFactory.ThisCannotTakeDamageWhile(
            conditions=True,
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            edisons_giant_robot
        ).SetCost(Cost("B")),
    ]

