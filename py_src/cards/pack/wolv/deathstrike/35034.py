from . import *

# * Lady Deathstrike

def GetAbilities() -> Sequence['Ability']:

    def lady_deathstrike(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        target = message.attacked
        player = target.GetOwner()
        if Player.IsType(player):
            player.DiscardRandomHandCards(1, effect)

    return [
        AbilityFactory.AfterUnitAttackAndDamageUnit(
            AbilityType.ForcedResponse,
            "This",
            "Character",
            lady_deathstrike
        ),
    ]

