from . import *

# * Marvel Boy: Noh-Var

def GetAbilities() -> Sequence['Ability']:

    def marvel_boy(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        message.GainPiercing(effect)
        message.GainRanged(effect)


    return [
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.Interrupt,
            "This",
            marvel_boy
        ).SetCost(Cost("R")),
    ]

