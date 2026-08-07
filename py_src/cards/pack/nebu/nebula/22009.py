from . import *

# Combat Ready

def GetAbilities() -> Sequence['Ability']:

    def combat_ready(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        def action(targets: Sequence['CardFace']):
            face = initiator.DiscardDeckUntil(
                effect,
                trait="TECHNIQUE",
                card_type=Upgrade
            )
            if face:
                face.PutIntoPlay(initiator, effect)
                face.ResolveSpecialAbility(initiator, effect)

        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Shuffle up to 2 [[technique]] upgrades from your discard pile into your deck",
                lambda targets:
                    Faces.ShuffleAllTo(targets, initiator.player_deck, effect)
            ).SetTarget(Upgrade, trait="TECHNIQUE", from_where=["YourDiscardPile"], range=(1, 2)),
            AbilityFactory.ForChoiceAbility(
                'Discard cards from the top of your deck until you discard a [[technique]] upgrade. Put that upgrade into play, then resolve its "Special" ability.',
                action
            )
        )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            combat_ready
        ).SetPlay().SetLabel(),
    ]

