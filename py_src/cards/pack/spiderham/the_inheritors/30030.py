from . import *

# Hunting the Spider-Totems

def GetAbilities() -> Sequence['Ability']:

    def hunting_the_spider_totems(effect: 'Effect', message: 'Message.WhenPhaseBegin') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        faces = Worlds.DiscardEncounterCards(3, effect)
        faces = Filter.ByType(faces, Minion, CardFinder2("INHERITOR"))

        if faces:
            player = Worlds.FindPlayer(PlayerFinder(controls=CardFinder2("WEB-WARRIOR", Unit2)), effect)
            if not player:
                player = Worlds.GetFirstPlayer(effect)

            for face in faces:
                face.EngagePlayer(player, effect)


    return [
        AbilityFactory.WhenPhaseBegin(
            AbilityType.ForcedInterrupt,
            "Villain",
            hunting_the_spider_totems
        ),
    ]

