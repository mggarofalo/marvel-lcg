from . import *

# * Baron Zemo's Sword

def GetAbilities() -> Sequence['Ability']:

    def baron_zemos_sword(effect: 'Effect', message: 'Message.AfterUnitDefeatedUnit') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        PlaceSecretCounterOnBoardMemberCard(Environment, 1, effect, with_fewest_secret=True)


    def baron_zemos_sword_action(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        Faces.RemoveCountersOn(effect.targets, 1, 'secret', effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Baron Zemo")
        ),
        AbilityFactory.AfterUnitAttackAndDefeatUnit(
            AbilityType.ForcedResponse,
            CardFinder(name="Baron Zemo"),
            None,
            baron_zemos_sword
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
            ex_operation=baron_zemos_sword_action
        ).SetCost(Cost("RRR", or_cost=Cost("YYY")))
        .SetTarget(Environment, trait="BOARD MEMBER", has_counter='secret', range=("Zero", 1)),
    ]

