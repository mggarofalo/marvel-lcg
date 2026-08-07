from . import *

# * Kid Omega: Quentin Quire

def GetAbilities() -> Sequence['Ability']:

    def kid_omega(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()

        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbilityWithCost(
                Cost("Y"),
                "Spend a [energy] resource → deal 1 damage to each enemy",
                lambda targets, res:
                    this.DealDamage(targets, 1, effect)
            ).SetTarget(Enemy, range="All"),
            AbilityFactory.ForChoiceAbilityWithCost(
                Cost("B"),
                "Spend a [mental] resource → remove 1 threat from each scheme",
                lambda targets, res:
                    this.RemoveThreatFromSchemes(targets, 1, effect)
            ).SetTarget(Scheme2, range="All")
        )


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            kid_omega
        ),
    ]

