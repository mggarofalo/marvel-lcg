from . import *

# Flash of Light

def GetAbilities() -> Sequence['Ability']:

    def flash_of_light(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        this.RemoveThreatFromSchemes(effect.targets, 3, effect)
        if effect.GetPaidResources().GetResourceIconTypes() >= 2:
            initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        Faces.GiveStatus(targets, "Confused", effect)
                ).SetTarget(Enemy, canbe_confused=True)
            )

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            flash_of_light
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2),
    ]

