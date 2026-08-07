from . import *

# * Amelia Voght

def GetAbilities() -> Sequence['Ability']:

    def amelia_voght(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetKillerPlayer()
        if player:
            identity = player.GetIdentity()
            if identity.IsConfused():
                this.PlaceThreatOnSchemes("MainScheme", 2, effect)
            else:
                Faces.GiveStatus([identity], "Confused", effect)

    return [
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            amelia_voght
        ),
    ]

