from . import *

# * Vision

def GetAbilities() -> Sequence['Ability']:

    def vision(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()

        def gain_2_atk(targets: Sequence['CardFace']) -> None:
            this.GainUntilPhaseEnd(effect, attack=2)
        def gain_2_thw(targets: Sequence['CardFace']) -> None:
            this.GainUntilPhaseEnd(effect, thwart=2)

        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "THW",
                gain_2_thw
            ),
            AbilityFactory.ForChoiceAbility(
                "ATK",
                gain_2_atk
            ),
        )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            vision
        ).SetCost(Cost("Y"))
        .LimitOncePerRound()
        # .SetDesc('choose THW or ATK. Until the end of the phase, Vision gets +2 to the chosen power')
    ]

