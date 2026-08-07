from . import *

# * Firebird: Bonita Juarez

def GetAbilities() -> Sequence['Ability']:

    def firebird_enterplay(effect: 'Effect', message: 'Message.WhenCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        if message.overpaid > 0:
            Faces.PlaceCountersOn([this], 1, 'rebirth', effect)

    def firebird(effect: 'Effect', message: 'Message.WhenUnitWouldBeDefeated') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        message.SetBeInstead(effect)
        this.HealHealth("All", effect)


    return [
        AbilityFactory.WhenCardEnterPlay(
            AbilityType.NonKeyword,
            "This",
            firebird_enterplay
        ),
        AbilityFactory.WhenUnitWouldBeDefeated(
            AbilityType.Interrupt,
            "This",
            firebird,
            by_consequential_damage=True,
        ).SetCostFunc(CostFunc.Counter("This", 1, 'rebirth')),
    ]

