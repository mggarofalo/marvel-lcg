from . import *

# Psychic Blast

def GetAbilities() -> Sequence['Ability']:

    def psychic_blast(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        identity = initiator.GetIdentity()

        this.DealDamage(effect.targets, 4, effect)

        if identity.HasTrait("UNLEASHED"):
            initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        this.DealDamage(targets, 4, effect)
                ).SetTarget("YourEngagedMinions", range="All"),
            )
            # We cannot use this, sometimes "34001a" change her form during this effect
            # this.DealDamage(effect.targets2, 4, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            psychic_blast,
        ).SetPlay().SetLabel()
        .SetTarget(Villain)
        .SetTarget2("YourEngagedMinions", range="All",
            condition=lambda effect:
                effect.GetInitiator().GetIdentity().HasTrait("UNLEASHED")
        ),
    ]

