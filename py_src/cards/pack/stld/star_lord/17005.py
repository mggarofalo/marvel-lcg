from . import *

# Sliding Shot

def GetAbilities() -> Sequence['Ability']:

    def sliding_shot(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        value = 5 + 2 * initiator.dealt_encounter_cards.GetSize()
        this.DealDamage(effect.targets, value, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            sliding_shot,
        ).SetPlay(only_if_you_control_face=CardFinder(name="Element Gun"))
        .SetLabel('attack')
        .SetTarget(Enemy)
    ]

