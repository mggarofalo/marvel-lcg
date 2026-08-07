from . import *

# Telepathic Suggestion

def GetAbilities() -> Sequence['Ability']:

    def telepathic_suggestion(effect: 'Effect', message: 'Message.WhenPlayerRevealCard') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        message.CancelWhenRevealedEffect(effect)

        psi_katana = GetYouControlPsiKatana(initiator)
        for _ in range(psi_katana):
            initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        this.DealDamage(targets, 2, effect)
                ).SetTarget(Enemy)
            )

        psi_knife = GetYouControlPsiKnife(initiator)
        for _ in range(psi_knife):
            initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        this.RemoveThreatFromSchemes(targets, 1, effect)
                ).SetTarget(Scheme2)
            )

    return [
        AbilityFactory.WhenPlayerRevealCard(
            AbilityType.HeroInterrupt,
            "You",
            None,
            "WhenRevealed",
            telepathic_suggestion,
            from_encounter=True,
        ).SetPlay().SetLabel(),
    ]

