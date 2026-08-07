from . import *

# "To Me, My X-Men!"

def GetAbilities() -> Sequence['Ability']:

    def to_me_my_x_men(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.PlayerDeckTop(
            effect,
            initiator,
            5,
            trait="X-MEN",
            card_type=Ally,
        )
        if face:
            face.PutIntoPlay(initiator, effect)

            def add_it_to_hand():
                if face.card.area.flags.is_in_play:
                    initiator.GainCard(face, effect)

            RunAt.TheEndOfThePhase(effect, add_it_to_hand)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            to_me_my_x_men
        ).SetPlay(only_if_your_identity_has_trait="X-MEN").SetLabel(),
    ]

