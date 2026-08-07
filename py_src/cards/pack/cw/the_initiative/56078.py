from . import *

# * Doc Samson

def GetAbilities() -> Sequence['Ability']:

    def doc_samson_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        if identity.IsConfused():
            this.PlaceThreatOnSchemes("MainScheme", 2, effect)
        else:
            Faces.GiveStatus([identity], "Confused", effect)

    def doc_samson_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        Faces.GiveStatus([identity], "Confused", effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            doc_samson_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            doc_samson_boost
        ),
    ]

