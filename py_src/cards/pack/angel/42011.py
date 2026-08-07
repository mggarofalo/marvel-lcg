from . import *

# * Elixir: Josh Foley

def GetAbilities() -> Sequence['Ability']:

    def elixir(effect: 'Effect', message: 'Message.AfterUnitAttackEnd|Message.AfterUnitThwartEnd') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.HealthUnits(effect.targets, 1, effect)


    return [
        AbilityFactory.CanPlayThisAllyCard(
        ).SetPlay(only_if_your_identity_has_one_of_traits=["X-FORCE", "X-MEN"]),
        AbilityFactory.AfterUnitAttackOrThwart(
            AbilityType.Response,
            "This",
            elixir
        ).SetTarget(Friend, canbe_heal=True, another=True),
    ]

