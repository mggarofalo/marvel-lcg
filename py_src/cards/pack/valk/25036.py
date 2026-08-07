from . import *

# Cosmic Alliance

def GetAbilities() -> Sequence['Ability']:

    def cosmic_alliance(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            cosmic_alliance,
        ).SetPlay().SetLabel()
        .SetTarget(Unit2,
            canbe_ready=True,
            traits=["AVENGER", "GUARDIAN"],
            select_rule="MustIncludeTraits",
            range=(2, 2)
        )
    ]

