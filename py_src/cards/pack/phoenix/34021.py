from . import *

# * Storm: Ororo Munroe

def GetAbilities() -> Sequence['Ability']:

    def storm(effect: 'Effect', message: 'Message.WhenSchemeBeingThwart') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        message.scheme.MoveThreat(2, effect, effect.targets)


    return [
        AbilityFactory.ReduceCostToPlayThis(
            1,
            your_identity_has_traits=["MUTANT", "X-MEN"],
        ),
        AbilityFactory.WhenUnitThwartScheme(
            AbilityType.Interrupt,
            "This",
            storm
        ).SetTarget(Scheme2, check_fn=lambda effect, face: face != effect.GetBindMessage(Message.WhenSchemeBeingThwart).scheme),
    ]

