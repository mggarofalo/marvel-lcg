from . import *

# * Butler

def GetAbilities() -> Sequence['Ability']:

    def butler_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        Faces.GiveStatus([player.GetIdentity()], "Confused", effect)

    return [
        AbilityFactory.PlaceThreatOnCardWhenThisScheme(
            AbilityType.ForcedInterrupt,
            CardFinder(name="Involuntary Procedures"),
            if_able=True
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            butler_boost
        ),
    ]

