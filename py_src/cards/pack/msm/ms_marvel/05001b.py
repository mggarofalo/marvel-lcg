from . import *

# * Kamala Khan

def GetAbilities() -> Sequence['Ability']:

    def kamala_khan(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()
        face = initiator.DiscardDeckUntil(
            effect,
            card_type=ClassCard,
            card_class="IdentitySpecific",
        )
        if face:
            initiator.GainCard(face, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            kamala_khan
        ).SetName("Teen Spirit")
        .LimitOncePerRound(),
    ]

