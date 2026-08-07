from . import *

# Bring the Hammer Down

def GetAbilities() -> Sequence['Ability']:

    def bring_the_hammer_down_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        face = Worlds.FindCardOnField(
            effect,
            name="Ronan the Accuser",
            card_type=Minion
        )
        if face:
            player = face.GetEngagedPlayer()
            face.DoActivate(player, effect)
        else:
            ThisCardGainSurge(effect)

    def bring_the_hammer_down_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        def action(face: 'CardFace'):
            player = Worlds.GetFirstPlayer(effect)
            player.DealEncounterCards(1, effect)

        message.would_atk_message.IfThisAttackDefeats(Unit2, action, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            bring_the_hammer_down_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            bring_the_hammer_down_boost,
            during_attack=True
        ),
    ]

