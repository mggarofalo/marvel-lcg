from . import *

# Innocent Bystanders

def GetAbilities() -> Sequence['Ability']:

    def innocent_bystanders(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Obligation)

        if message.attacker.GetControlByOrOwner().IsPlayer():
            player = message.attacker.GetControlByPlayer()
        else:
            player = message.attacked.GetControlByPlayer()

        if Worlds.IsExpert(effect):
            value = 2
        else:
            value = 1

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbilityToSpend(
                Cost("1")
            ),
            AbilityFactory.ForChoiceAbility(
                f"Place {value} threat on the main scheme",
                lambda targets:
                    this.PlaceThreatOnSchemes(targets, value, effect)
            ).SetTarget(MainScheme),
        )

        Faces.RemoveCountersOn([this], 1, 'bystander', effect)


    return [
        AbilityFactory.AfterUnitAttackUnit(
            AbilityType.ForcedResponse,
            "You",
            Enemy,
            innocent_bystanders,
        ),
        AbilityFactory.AfterUnitAttackUnit(
            AbilityType.ForcedResponse,
            Enemy,
            "You",
            innocent_bystanders,
        ),
    ]

