from . import *

# Gamma Blast

def GetAbilities() -> Sequence['Ability']:

    def gamma_blast(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        overkill = initiator.form.IsInEnergyForm("Gamma")
        initiator.form.ChangeEnergyForm(effect, "Gamma")
        this.DealDamage(effect.targets, 7, effect, property=AttackProperty(overkill=overkill))


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            gamma_blast
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

