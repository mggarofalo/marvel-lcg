from . import *

# Psychic Kicker

def GetAbilities() -> Sequence['Ability']:

    def psychic_kicker(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)
        def action(power_effect: 'Effect', power_message: 'Message.WhenUnitUseBasicPower'):
            ally.TemporaryGain(
                effect,
                power_message.would_message,
                thwart=+2,
                attack=+2,
            )

        for ally in effect.targets:
            this.effect.RegisterTemp(
                AbilityFactory.WhenUnitUseBasicPower(
                    AbilityType.Temp0,
                    ally,
                    action,
                    powers=["ATK", "THW"]
                ),
                unregister_after_exec=True,
                until_round_end=True
            )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            psychic_kicker
        ).SetPlay(only_if_your_identity_has_trait="PSIONIC").SetLabel()
        .SetTarget(Ally),
    ]

