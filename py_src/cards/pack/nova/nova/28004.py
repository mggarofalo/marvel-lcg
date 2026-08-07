from . import *

# Lightspeed Flight

def GetAbilities() -> Sequence['Ability']:

    def lightspeed_flight(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 3, effect)


    return [
        AbilityFactory.UpdateResourcesGeneratedWhilePlayingThis(
            color="G",
            update_rule="Double"
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            lightspeed_flight,
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2),
    ]

