from . import *

# Coup de Foudre

def GetAbilities() -> Sequence['Ability']:

    def coup_de_foudre(effect: 'Effect', message: 'Message.WhenMinionEngagePlayer') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.engaged_player

        value = FacesCounter.CountTotalBoostIcons([message.minion])
        faces = player.DiscardDeckTopCards(value, effect)

        threat = FacesCounter.GetPrintedResourcesCount(faces, "Y")
        this.PlaceThreatOnSchemes([this], threat, effect)


    return [
        AbilityFactory.WhenMinionEngagePlayer(
            AbilityType.ForcedInterrupt,
            Minion,
            None,
            coup_de_foudre
        ),
    ]

