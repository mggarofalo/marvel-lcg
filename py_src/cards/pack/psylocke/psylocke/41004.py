from . import *

# Flurry of Blades

def GetAbilities() -> Sequence['Ability']:

    def flurry_of_blades(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        this.DealDamage(effect.targets, 2, effect)

        psi_knife = GetYouControlPsiKnife(initiator)
        for _ in range(psi_knife):
            initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        Faces.GiveStatus(targets, "Confused", effect)
                ).SetTarget(Enemy, canbe_confused=True)
            )

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

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            flurry_of_blades
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

