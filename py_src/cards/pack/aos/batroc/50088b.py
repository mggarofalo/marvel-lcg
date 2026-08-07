from . import *

# Locate Missing Person

def GetAbilities() -> Sequence['Ability']:

    def locate_missing_person(effect: 'Effect', message: 'Message.AfterCardRemovedToken') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        player = Worlds.FindPlayer(None, effect)

        allies = Worlds.GetSetAsideAreaCards(effect, CardFinder(
            name="Rescued Captive",
            card_type=Ally,
        ))
        if allies and player:
            ally = allies[0]
            ally.PutIntoPlay(player, effect, under_control=True, exhaust=True)

        player = Worlds.GetFirstPlayer(effect)
        text = player.MayChooseOneText(["Advance to stage 3A"])
        if not text:
            this.PlaceThreatOnSchemes([this], "3*", effect)
        else:
            this.Advance("3A", effect)

    return [
        AbilityFactory.WhenLastThreatRemoveFromThisScheme(
            AbilityType.ForcedInterrupt,
            locate_missing_person
        ),
    ]

