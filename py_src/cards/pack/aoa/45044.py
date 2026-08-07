from . import *

# Test the Defense

def GetAbilities() -> Sequence['Ability']:

    def test_the_defense(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.PlaceCountersOn([this], 1, 'test', effect)

        if this.GetCounters('test') >= 5:
            Faces.DiscardAll([this], effect)
            this.DealDamage(effect.targets2, 5, effect)


    return [
        AbilityFactory.AfterPlayerPlayedCard(
            AbilityType.Response,
            "You",
            CardFinder2("ATTACK", Event),
            test_the_defense,
        )
        .SetTarget("This")
        .SetTarget2(Enemy),
    ]

