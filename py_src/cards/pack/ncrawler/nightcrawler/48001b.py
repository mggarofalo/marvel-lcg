from . import *

# * Kurt Wagner

def GetAbilities() -> Sequence['Ability']:

    def kurt_wagner(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            name="Bamf!"
        )
        if face:
            Faces.AddToHand([face], initiator, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            kurt_wagner
        ).LimitOncePerRound(),
    ]

