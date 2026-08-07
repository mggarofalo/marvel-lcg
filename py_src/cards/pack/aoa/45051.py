from . import *

# Basic Spell

def GetAbilities() -> Sequence['Ability']:

    def basic_spell(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Heal 3 damage from an identity",
                lambda targets:
                    this.HealthUnits(targets, 3, effect)
            ).SetTarget(Identity, canbe_heal=True),
            AbilityFactory.ForChoiceAbility(
                "Remove 3 threat from a scheme",
                lambda targets:
                    this.RemoveThreatFromSchemes(targets, 3, effect)
            ).SetTarget(Scheme2),
            AbilityFactory.ForChoiceAbility(
                "Deal 3 damage to an enemy",
                lambda targets:
                    this.DealDamage(targets, 3, effect)
            ).SetTarget(Enemy)
        )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            basic_spell
        ).SetPlay(only_if_your_identity_has_trait="MYSTIC").SetLabel(),
    ]

