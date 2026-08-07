from . import *

# Gunboat Diplomacy

def GetAbilities() -> Sequence['Ability']:

    def gunboat_diplomacy(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        thwart = 0
        faces = effect.cost_func.Get(CostFunc.Exhaust).return_exhausted_cards
        faces = Filter.ByType(faces, HasThwart)
        for face in faces:
            thwart += face.thwart

        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "",
                lambda targets:
                    this.RemoveThreatFromSchemes(targets, 1, effect)
            ).SetLabel('thwart')
            .SetTarget(Scheme2, range=(0, thwart), repeat_rules="Threat")
        )
        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "",
                lambda targets:
                    this.DealDamage(targets, 1, effect)
            ).SetLabel('attack')
            .SetTarget(Enemy, range=(0, thwart), repeat_rules="Health")
        )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            gunboat_diplomacy
        ).SetPlay().SetLabel('attack', 'thwart')
        .SetCostFunc(CostFunc.Exhaust(Select.Alliance(["X-FORCE", "X-MEN"])))
        .SetTarget2(Scheme2)
        .SetTarget2(Enemy)
    ]

