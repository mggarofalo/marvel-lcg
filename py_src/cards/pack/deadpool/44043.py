from . import *

# * Bob, Agent of Hydra: Bob Dobalina

def GetAbilities() -> Sequence['Ability']:

    def bob_agent_of_hydra(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()

        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Deal 2 damage to an enemy",
                lambda targets:
                    this.DealDamage(targets, 2, effect)
            ).SetTarget(Enemy),
            AbilityFactory.ForChoiceAbility(
                "Remove 1 threat from a scheme",
                lambda targets:
                    this.RemoveThreatFromSchemes(targets, 1, effect)
            ).SetTarget(Scheme2),
        )


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            bob_agent_of_hydra
        )
        .SetTarget2(Enemy)
        .SetTarget2(Scheme2),
    ]

