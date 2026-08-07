from . import *

# * Bulldozer's Helmet

def GetAbilities() -> Sequence['Ability']:

    def bulldozers_helmet(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()
        player.DiscardDeckTopCards(message.dealt_damage, effect)

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name=BULLDOZER)
        ),
        AbilityFactory.AfterUnitAttackUnit(
            AbilityType.ForcedResponse,
            "AttachedVillain",
            "You",
            bulldozers_helmet,
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCostFunc(CostFunc.Discard("RandomHandCard"))
        .SetCostFunc(CostFunc.Exhaust("YourHero"))
    ]

