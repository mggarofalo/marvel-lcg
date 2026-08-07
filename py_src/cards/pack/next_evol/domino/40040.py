from . import *

# A Good Workout

def GetAbilities() -> Sequence['Ability']:

    def a_good_workout(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        value = 4
        face = initiator.DiscardDeckTopCard(effect)
        value += FacesCounter.GetPrintedResourcesIcon([face], initiator.player_deck)

        this.DealDamage(effect.targets, value, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            a_good_workout
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

