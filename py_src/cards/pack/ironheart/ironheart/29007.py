from . import *

# New and Improved

def GetAbilities() -> Sequence['Ability']:

    def new_and_improved(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        value = GetIronheartVersionNumber(initiator)

        def action1(targets: Sequence['CardFace']):
            face = Search.PlayerCard(
                effect,
                initiator,
                include_player_deck=True,
                card_class="IdentitySpecific",
            )
            if face:
                Faces.AddToHand([face], initiator, effect)

        def action2(targets: Sequence['CardFace']):
            Faces.GiveStatus(targets, "Tough", effect)

        def action3(targets: Sequence['CardFace']):
            Faces.ReadyAll(targets, effect)

        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Search your deck for an Ironheart card and add it to your hand",
                action1
            ),
            AbilityFactory.ForChoiceAbility(
                "Give Ironheart a tough status card",
                action2
            ).SetTarget(name="Ironheart", canbe_tough=True),
            AbilityFactory.ForChoiceAbility(
                "Ready Ironheart",
                action3
            ).SetTarget(name="Ironheart", canbe_ready=True),
            choose_x_different_options=value
        )

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            new_and_improved
        ).SetPlay().SetLabel(),
    ]

