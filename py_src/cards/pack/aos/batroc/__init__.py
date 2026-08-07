from cards.pack import *

def GetAlertLevel(effect: 'Effect') -> 'Environment|None':
    return Worlds.FindCardOnField(effect, name="Alert Level", card_type=Environment)

def IfAlertLevelIsHighSide(effect: 'Effect') -> bool:
    return Worlds.FindCardOnField(effect, name="Alert Level", trait="HIGH") != None

def WhenDefeatedPlaceThreatOnAlertLevel() -> 'Ability':

    def embassy_patrol(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        env = GetAlertLevel(effect)
        if env:
            Faces.PlaceTokensOn([env], 1, 'threat', effect)

    return AbilityFactory.WhenUnitBeDefeated(
        AbilityType.WhenDefeated,
        "This",
        embassy_patrol
    )

