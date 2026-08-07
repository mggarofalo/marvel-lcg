from . import *

# Royal Flush

def GetAbilities() -> Sequence['Ability']:

    def royal_flush(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.PlaceCountersOn(effect.targets, 1, 'charge', effect)

        initiator = effect.GetInitiator()

        def action():
            initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        this.DealDamage(targets, 0, effect)
                ).SetTarget(Enemy)
            )

        for _ in range(3):
            action()


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            royal_flush
        ).SetPlay().SetLabel('attack')
        .SetTarget(name="Gambit")
        .SetTarget2(Enemy),
    ]

