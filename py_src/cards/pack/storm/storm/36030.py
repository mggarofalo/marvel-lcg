from . import *

# Claustrophobia

def GetAbilities() -> Sequence['Ability']:

    def claustrophobia(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Obligation)
        player = this.GetGaveToPlayer()

        identity = player.GetIdentity()
        identity.ChangeToForm(AlterEgo, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            claustrophobia
        ),
        AbilityFactory.PlayersCannotChangeForms(
            AbilityType.NonKeyword,
            "AttachedPlayer"
        ),
        AbilityFactory.PlayerActionToRemoveThisFromGame(
            AbilityType.AlterEgoAction
        ).SetCostFunc(CostFunc.Exhaust("YourIdentity")),
    ]

