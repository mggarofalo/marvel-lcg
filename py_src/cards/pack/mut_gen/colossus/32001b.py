from . import *

# * Piotr Rasputin

def GetAbilities() -> Sequence['Ability']:

    def piotr_rasputin_setup(effect: 'Effect', message: 'Message.WhenCardSetup') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            include_discard_pile=True,
            name="Organic Steel",
            card_type=Upgrade,
        )
        if face:
            initiator.GainCard(face, effect)

    def piotr_rasputin(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()
        Faces.ShuffleAllTo(effect.targets, initiator.player_deck, effect)


    return [
        AbilityFactory.WhenCardSetup(
            "This",
            piotr_rasputin_setup
        ),
        AbilityFactory.AfterUnitChangeForm(
            AbilityType.Response,
            "You",
            piotr_rasputin,
            to_this_form=True,
        ).SetName("Aspiring Artist")
        .SetTarget(card_class="IdentitySpecific", from_where=["YourDiscardPile"])
    ]

