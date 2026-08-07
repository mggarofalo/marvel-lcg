from . import *

# * Corvus Glaive

def GetAbilities() -> Sequence['Ability']:

    def corvus_glaive(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        targets = effect.cost_func.Get(CostFunc.Discard).return_discarded_cards
        damage = FacesCounter.CountTotalBoostIcons(targets)
        DealDamageToAvengersTower(damage, effect)

    return [
        AbilityFactory.AfterUnitAttackUnit(
            AbilityType.ForcedResponse,
            "This",
            None,
            corvus_glaive,
            is_undefended_attack=True
        ).SetCostFunc(CostFunc.Discard("EncounterDeckTop")),
        *AbilityFactory.UnitCannotBeDefeatedWhile(
            AbilityType.NonKeywordBold,
            "This",
            condition=lambda effect:
                CheckVillainHealth("Proxima Midnight", effect) > 0,
            change_on_event=OnEvent.Health(Villain),
        ),
        AbilityFactory.AfterUnitBeDefeated(
            AbilityType.NonKeyword,
            CardFinder(name="Proxima Midnight"),
            lambda effect, message:
                effect.this.CastTo(EncounterVillain).Advance(effect)
        ),
    ]

