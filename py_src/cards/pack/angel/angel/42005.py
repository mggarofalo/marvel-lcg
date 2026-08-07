from . import *

# Metamorphosis

def GetAbilities() -> Sequence['Ability']:

    def metamorphosis(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.GetIdentity().ChangeToOtherForm(effect)
        if initiator.GetIdentity().IsName("Warren Worthington III"):
            initiator.DrawUp(1, effect)
        if initiator.GetIdentity().IsName("Angel"):
            initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        this.RemoveThreatFromSchemes(targets, 2, effect)
                ).SetTarget(Scheme2)
            )
        if initiator.GetIdentity().IsName("Archangel"):
            initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        this.DealDamage(targets, 3, effect)
                ).SetTarget(Enemy)
            )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            metamorphosis
        ).SetPlay().SetLabel(),
    ]

