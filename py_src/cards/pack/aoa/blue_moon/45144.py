from . import *

# * Warstar

def GetAbilities() -> Sequence['Ability']:

    def warstar_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        face = Worlds.DiscardEncounterTopCard(effect)
        if face and face.HasTrait("IMPERIAL GUARD"):
            face.Reveal(player, effect)
        else:
            ResolveSpecialAbilityOnTheSETTINGEnvironment(player, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            warstar_revealed
        ),
    ]

