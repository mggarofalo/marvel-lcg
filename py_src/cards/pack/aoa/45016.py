from . import *

# Side-by-Side

def GetAbilities() -> Sequence['Ability']:

    def side_by_side(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)

        def gain_thw_atk(faces: Sequence['CardFace']):
            for face in faces:
                face.GainUntilPhaseEnd(
                    effect,
                    thwart=1,
                    attack=1
                )

        both = effect.targets + list(effect.cost_func.Get(CostFunc.Ready).return_readied_cards)

        initiator = effect.GetInitiator()
        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Heal 1 damage from both characters",
                lambda targets:
                    this.HealthUnits(targets, 1, effect)
            ).SetTarget(both, range="All", canbe_heal=True),
            AbilityFactory.ForChoiceAbility(
                "Both characters get +1 THW and +1 ATK until the end of the phase",
                gain_thw_atk
            ).SetTarget(both, range="All"),
        )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            side_by_side
        ).SetPlay().SetLabel()
        .SetCostFunc(CostFunc.Ready("YourSidekick"))
        .SetTarget("YourHero")
        .SetHasNoTargetEffect(),
    ]

