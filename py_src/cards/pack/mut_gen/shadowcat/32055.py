from . import *

# Permanently Phased

def GetAbilities() -> Sequence['Ability']:

    def permanently_phased(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        player.form.ChangeMassForm(effect, "Phased")

    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            permanently_phased
        ),
        *AbilityFactory.UnitCannotAttackTarget(
            "AttachedIdentity",
            cannot_attack=True,
            cannot_trigger_attack_ability=True
        ),
        *AbilityFactory.UnitCannotDefend(
            "AttachedIdentity",
            None,
            cannot_trigger_defense_ability=True
        ),
        AbilityFactory.PlayersCannotChangeAdditionalForms(
            "AttachedPlayer",
            exclude="This"
        ),
        AbilityFactory.PlayerActionToRemoveThisFromGame(
            AbilityType.AlterEgoAction,
        ).SetCostFunc(CostFunc.Exhaust(name="Kitty Pryde")),
    ]

