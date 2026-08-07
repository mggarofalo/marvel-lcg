from . import *

# Coup de Grâce

def GetAbilities() -> Sequence['Ability']:

    def coup_de_grace(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.DefeatUnits(effect.targets, this, effect, ignore_when_defeated=True)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            coup_de_grace
        ).SetPlay().SetLabel('attack')
        .SetTarget(Minion, with_attach=Upgrade, non_trait="ELITE"),
    ]

