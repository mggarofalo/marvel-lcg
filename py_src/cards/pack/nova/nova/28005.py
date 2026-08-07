from . import *

# Pot Shot

def GetAbilities() -> Sequence['Ability']:

    def pot_shot(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 4, effect)


    return [
        AbilityFactory.UpdateResourcesGeneratedWhilePlayingThis(
            color="G",
            update_rule="Double"
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            pot_shot
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

