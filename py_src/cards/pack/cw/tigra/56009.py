from . import *

# Feline Senses

def GetAbilities() -> Sequence['Ability']:

    def feline_senses(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        def action():
            initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        Faces.GiveStatus(targets, "Stunned", effect)
                ).SetTarget(Minion, canbe_stunned=True)
            )

        this.RemoveThreatFromSchemes(effect.targets, 3, effect, if_this_remove_last_threat=action)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            feline_senses
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2),
    ]

