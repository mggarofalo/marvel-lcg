from . import *

# * Odinson

def GetAbilities() -> Sequence['Ability']:

    def odinson(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            include_discard_pile=True,
            card_type=Upgrade,
            card_class="IdentitySpecific",
            name="Mjolnir",
        )
        if face:
            initiator.GainCard(face, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            odinson
        ).SetName("Worthy")
        .LimitOncePerRound(),
    ]

