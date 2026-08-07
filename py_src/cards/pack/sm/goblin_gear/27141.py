from . import *

# Remote Navigation

def GetAbilities() -> Sequence['Ability']:

    def remote_navigation_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        attach = Worlds.FindCardOnField(
            effect,
            name="Advanced Glider",
            card_type=Attachment
        )
        if attach:
            villain = Worlds.FindVillain(effect)
            if villain:
                villain.DoActivate(player, effect)
        else:
            face = Search.EncounterCard(
                effect,
                include_discard_pile=True,
                name="Advanced Glider",
                card_type=Attachment,
            )
            if face:
                face.Reveal(None, effect)

    def remote_navigation_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        # assert Villain.IsType(message.for_message.trigger)
        # message.for_message.trigger.GiveFacedownBoostCards(2, effect)
        # Give the villain 2 additional boost cards for this activation.
        # Why is the villain not the activater?
        # effect.world.GetVillain(effect).GiveFacedownBoostCards(2, effect)
        message.GiveBoostCardForThisActivation(Villain, 2, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            remote_navigation_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            remote_navigation_boost
        )
    ]

