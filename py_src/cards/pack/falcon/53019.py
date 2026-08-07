from . import *

# Strength in Diversity

def GetAbilities() -> Sequence['Ability']:

    def strength_in_diversity(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        faces = Worlds.GetOnFieldFriendlyCharacters(effect)
        value = Faces.CountTraitNum(faces)

        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                f"Remove 1 threat from a scheme",
                lambda targets:
                    this.RemoveThreatFromSchemes(targets, 1, effect)
            ).SetTarget(Scheme2),
            AbilityFactory.ForChoiceAbility(
                f"Deal 1 damage to an enemy",
                lambda targets:
                    this.DealDamage(targets, 1, effect)
            ).SetTarget(Enemy),
            repeat=value
        )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            strength_in_diversity
        ).SetPlay().SetLabel(),
    ]

