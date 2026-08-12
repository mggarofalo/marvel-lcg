from . import *

# Sibling Rivalry

def GetAbilities() -> Sequence['Ability']:

    def sibling_rivalry(effect: 'Effect', message: 'Message.AfterPhaseBegin') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = Worlds.FindPlayerByName("Gamora", effect)
        if player:
            player.DealEncounterCards(1, effect)


    return [
        AbilityFactory.PlayersCannotRemoveThreatFrom(
            PlayerFinder(non_name="Gamora"),
            "This"
        ),
        # The card prints "Forced Response: After the villain phase begins".
        # This said `AbilityType.WhenDefeated` until MARVEL-89, which maps to
        # TimingPriority.Boost (6) rather than ForcedResponse (7) -- so it
        # resolved a level early and the UI labelled it "When Defeated".
        # Nothing about this card is a When Defeated, a When Revealed or a
        # boost, and no other ability in any pack registers WhenDefeated on
        # AfterPhaseBegin.
        AbilityFactory.AfterPhaseBegin(
            AbilityType.ForcedResponse,
            "Villain",
            sibling_rivalry,
        ),
    ]

