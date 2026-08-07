from . import *

# Recruitment Drive

def GetAbilities() -> Sequence['Ability']:

    def recruitment_drive(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        player = effect.targets[0].GetControlByPlayer()
        assert player
        Worlds.UpdateNextCardPlayCost(
            player,
            0,
            effect,
            finder=CardFinder(card_type=Ally),
            update_to=True,
            in_this="Phase"
        )

    return [
        AbilityFactory.CanPlayThisSupportCard(
        ).SetPlay(alter_ego_form_only=True),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            recruitment_drive
        ).SetTarget("Players")
        .SetCostFunc(CostFunc.Discard("This")),
    ]

