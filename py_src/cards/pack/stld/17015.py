from . import *

# Blaze of Glory

def GetAbilities() -> Sequence['Ability']:

    def blaze_of_glory(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Players.EachCharacterGetsUntilPhaseEnd(
            None,
            effect,
            CardFinder2("GUARDIAN", Unit2),
            thwart=+2,
            attack=+2,
        )

        def deal_damage():
            units = Worlds.GetOnFieldCharacters(effect, CardFinder2("GUARDIAN", Unit2))
            # Because `this` is Event, it will call Attack on `DealDamage`
            # this.DealDamage(units, 1, effect)
            for unit in units:
                unit.TakeDamage(this, 1, effect)

        RunAt.TheEndOfThePhase(effect, deal_damage)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            blaze_of_glory
        ).SetPlay().MaxOnePerRound()
    ]

