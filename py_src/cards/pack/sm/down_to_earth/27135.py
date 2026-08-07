from . import *

# Loose Ends

def GetAbilities() -> Sequence['Ability']:

    def loose_ends_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        def discard_random_hand_cards(change_effect: 'Effect', change_message: 'Message.AfterUnitChangeForm') -> None:
            player.DiscardRandomHandCards(1, effect)

        change_effects = this.effect.Registers(
            AbilityFactory.AfterUnitChangeForm(
                AbilityType.Temp0,
                None,
                discard_random_hand_cards,
                conditions=[
                    lambda effect, message:
                        player.GetIdentity() == message.trigger,
                ]
            )
        )


        do_reveal = False

        face = Search.SearchForCard(
            effect,
            player,
            include_encounter_deck=True,
            include_encounter_discard_pile=True,
            include_set_aside=True,
            include_removed_area=True,
            card_type=Obligation,
            obligation_give_to=player,
        )
        if face:
            face.Reveal(player, effect)
            do_reveal = True

        Effects.UnRegister(change_effects)

        if not do_reveal:
            ThisCardGainSurge(effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            loose_ends_revealed
        ),
    ]

