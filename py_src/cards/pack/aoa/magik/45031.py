from . import *

# * Colossus: Piotr Rasputin

def GetAbilities() -> Sequence['Ability']:

    def colossus(effect: 'Effect', message: 'Message.WhenUnitBeingAttack') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        # Had pay effect during trigger this effect
        initiator.PlayCardsLikeInTurn([this], effect, ignore_resources_cost=True, forced=True)
        if this.IsInPlay():
            message.DeclareDefender(this, effect)


    return [
        # Without this, this ally cannot be play
        AbilityFactory.CanPlayThisAllyCard(
        ),
        AbilityFactory.WhenUnitBeingAttack(
            AbilityType.Interrupt,
            "You",
            Enemy,
            colossus,
        ).SetPlay(),
    ]

