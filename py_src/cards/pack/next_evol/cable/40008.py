from . import *

# * Professor

def GetAbilities() -> Sequence['Ability']:

    def professor(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()

        def action(targets: Sequence['CardFace']):
            face = Search.PlayerCard(
                effect,
                initiator,
                include_player_deck=True,
                include_discard_pile=True,
                card_type=PlayerSideScheme
            )
            if face:
                initiator.GainCard(face, effect)


        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Draw 1 card",
                lambda targets:
                    Players.DrawUp(targets, 1, effect)
            ).SetTarget("YourIdentity"),
            AbilityFactory.ForChoiceAbility(
                "Search your deck and discard pile for a player side scheme and add it to your hand",
                action
            )
        )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            professor
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

