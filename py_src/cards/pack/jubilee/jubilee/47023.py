from . import *

# Grounded

def GetAbilities() -> Sequence['Ability']:

    def grounded(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Obligation)
        player = this.GetGaveToPlayer()

        player.GetIdentity().ChangeToForm(AlterEgo, effect)


    return [
        AbilityFactory.AdditionalCostToChangeForm(
            "You",
            CostFunc.Spend(Cost("2", same_type=True)),
            to_form=Hero,
            during_your_turn=True,
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            grounded
        ),
        AbilityFactory.AfterPlayerPlayedCard(
            AbilityType.Response,
            "You",
            CardFinder(card_class="IdentitySpecific", card_type=Event),
            RemoveThisFromGame
        ),
    ]

