from . import *

# * Moira MacTaggert

def GetAbilities() -> Sequence['Ability']:

    def moira_mactaggert(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        player = message.trigger.GetControlByPlayer()
        player.DrawUp(1, effect)


    return [
        AbilityFactory.CanPlayThisSupportCard(
        ).SetPlay(only_if_your_identity_has_trait="MUTANT"),
        AbilityFactory.AfterUnitChangeForm(
            AbilityType.Response,
            None,
            moira_mactaggert,
            to_form=Hero,
            from_form=CardFinder2("MUTANT", AlterEgo)
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

