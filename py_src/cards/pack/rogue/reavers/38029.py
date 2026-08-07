from . import *

# * Donald Pierce

def GetAbilities() -> Sequence['Ability']:

    def donald_pierce(effect: 'Effect', message: 'Message.AfterMinionEngagePlayer') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        face = Worlds.GetEncounterDiscardPile(effect).FindTopmost(CardFinder2("REAVER", Minion))
        if face:
            face.Reveal(player, effect)


    return [
        AbilityFactory.AfterMinionEngagePlayer(
            AbilityType.ForcedResponse,
            "This",
            "You",
            donald_pierce
        ),
    ]

