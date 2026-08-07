from . import *

# Heart-Shaped Herb

def GetAbilities() -> Sequence['Ability']:

    def heart_shaped_herb_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)
        player = message.GetToPlayer()
        villain = Worlds.FindVillain(effect)
        units = [villain] if villain else []
        units += player.GetEngagedMinions()
        Faces.GiveStatus(units, "Tough", effect)

    def heart_shaped_herb_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)
        villian = Worlds.FindVillain(effect)
        if villian:
            Faces.GiveStatus([villian], "Tough", effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            heart_shaped_herb_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            heart_shaped_herb_boost
        ),
    ]

