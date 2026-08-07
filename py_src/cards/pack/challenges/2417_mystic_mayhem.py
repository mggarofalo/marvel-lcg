from . import *

# Mystic Mayhem

def GetAbilities() -> Sequence['Ability']:

    def mystic_mayhem(effect: 'Effect', message: 'Message.WhenPhaseEnd'):
        this = effect.this.CastTo(Challenge)
        Unused(this)

        player = Worlds.GetFirstPlayer(effect)

        face = Worlds.GetEncounterDeck(effect).GetTop()
        if face:
            face.Reveal(player, effect)

    return [
        AbilityFactory.UsingOnlyHeroes(
            has_traits=["MYSTIC"]
        ),
        AbilityFactory.WhenVillainPhaseEnd(
            AbilityType.Challenge,
            mystic_mayhem
        )
    ]

