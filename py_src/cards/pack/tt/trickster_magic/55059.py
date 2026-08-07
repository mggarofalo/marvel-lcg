from . import *

# * Zzzax

def GetAbilities() -> Sequence['Ability']:

    def zzzax_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        faces = player.DiscardDeckTopCards(4, effect)
        size = FacesCounter.GetPrintedResourcesCount(faces, "Y")
        identity.TakeIndirectDamage(this, size, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            zzzax_revealed
        ),
        ThePlayerWhoDefeatedThisMinionPutsTheSetAsideAllyIntoPlayUnderTheirControl("Zzzax")
    ]

