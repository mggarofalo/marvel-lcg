from . import *

# * Gamora

def GetAbilities() -> Sequence['Ability']:

    def gamora_enter_play(effect: 'Effect', message: 'Message.WhenCardWouldMoveToArea') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        faces = Worlds.FindCardsOnField(
            effect,
            card_type=Ally,
            name="Gamora"
        )
        Faces.DiscardAll(faces, effect)

    def gamora(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        player.DiscardControlCards(effect, upgrade=True)


    return [
        AbilityFactory.WhenCardWouldEnterPlay(
            AbilityType.ForcedInterrupt,
            "This",
            gamora_enter_play,
        ),
        AbilityFactory.AfterUnitAttackAndDamageUnit(
            AbilityType.ForcedResponse,
            "This",
            "You",
            gamora
        ),
    ]

