from . import *

# Protect the Planet

def GetAbilities() -> Sequence['Ability']:

    def protect_the_planet(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        player = message.GetToPlayer()
        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Remove 3 threat from this scheme",
                lambda targets:
                    this.RemoveThreatFromSchemes(targets, 3, effect)
            ).SetTarget([this], has_threat=True),
            AbilityFactory.ForChoiceAbility(
                "Deal 3 damage to a minion",
                lambda targets:
                    this.DealDamage(targets, 3, effect)
            ).SetTarget(Minion),
        )


    return [
        AbilityFactory.AfterResolveVillainPhaseStep(
            AbilityType.ForcedResponse,
            1,
            ResolveTheBadoonShipChargeUpAbility
        ),
        ExhaustTheMilano(
            None,
            protect_the_planet
        )
    ]

