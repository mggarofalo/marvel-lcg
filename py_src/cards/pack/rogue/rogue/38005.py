from . import *

# Goin' Rogue

def GetAbilities() -> Sequence['Ability']:

    def goin_rogue(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        identity = initiator.GetIdentity()

        value = 3
        if identity.HasTrait("AERIAL"):
            value += 2

        this.RemoveThreatFromSchemes(effect.targets, value, effect)

        if identity.retaliate:
            initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        Faces.GiveStatus(targets, "Confused", effect)
                ).SetTarget(Enemy, canbe_confused=True)
            )
        if identity.IsStalwart():
            initiator.DrawUp(1, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            goin_rogue
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2),
    ]

