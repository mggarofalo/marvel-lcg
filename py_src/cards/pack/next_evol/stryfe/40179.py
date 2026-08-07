from . import *

# Telekinetic Wave

def GetAbilities() -> Sequence['Ability']:

    def telekinetic_wave_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        faces = player.GetControlCardsByType(upgrade=True, support=True)
        face = player.AskChooseFace(faces, effect)
        if face:
            Faces.ReturnToHand([face], player, effect)

        stryfe = Worlds.FindCardOnField(
            effect,
            name="Stryfe",
            card_type=Enemy
        )
        if stryfe:
            stryfe.DoActivate(player, effect)

    def telekinetic_wave_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        if FacesCounter.GetMaxSameTypeCount(player.hand_cards.Get()) >= 3:
            this.PlaceThreatOnSchemes("MainScheme", 3, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            telekinetic_wave_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            telekinetic_wave_boost
        ),
    ]

