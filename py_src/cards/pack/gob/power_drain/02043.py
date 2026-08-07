from . import *

# Electromagnetic Pulse

def GetAbilities() -> Sequence['Ability']:

    def electromagnetic_pulse_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        discarded = False

        faces = Worlds.DiscardEncounterCards(7, effect)
        for face in faces:
            if face.IsName("Electro"):
                face.PutIntoPlay(player, effect)
                discarded = True

        if not discarded:
            ThisCardGainSurge(effect)

    def electromagnetic_pulse_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        Worlds.DiscardEncounterCards(3, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            electromagnetic_pulse_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            electromagnetic_pulse_boost
        ),
    ]

