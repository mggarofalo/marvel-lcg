from . import *

# Rough Riders

def GetAbilities() -> Sequence['Ability']:

    def rough_riders_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        villain = Worlds.FindVillain(effect)
        if villain:
            villain.Gain(effect, +1, considered_at_least_hp=1)
            Faces.ResolveAbility([villain], player, AbilityType.ForcedResponse, effect)
            villain.Gain(effect, -1, considered_at_least_hp=1)

        villain = GetNextVillain(effect, villain)
        villain.SetActive(effect)
        villain.Gain(effect, +1, considered_at_least_hp=1)
        Faces.ResolveAbility([villain], player, AbilityType.ForcedResponse, effect)
        villain.Gain(effect, -1, considered_at_least_hp=1)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            rough_riders_revealed
        ),
    ]

