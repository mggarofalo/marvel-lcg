from . import *

# Kraven the Hunter

def GetAbilities() -> Sequence['Ability']:

    def kraven_the_hunter(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        player.DiscardControlCards(effect, upgrade=True, support=True)


    return [
        AbilityFactory.AfterUnitAttackAndDamageUnit(
            AbilityType.ForcedResponse,
            "This",
            "YouControlCharacter",
            kraven_the_hunter
        ),
    ]

