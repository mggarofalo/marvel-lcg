from . import *

# Dead to Rights

def GetAbilities() -> Sequence['Ability']:

    def dead_to_rights_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()
        if not Faces.ExhaustAll([identity], effect):
            this.PlaceThreatOnSchemes("MainScheme", 2, effect)

    def dead_to_rights_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        message.GiveBoostCardForThisActivation(Villain, 1, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            dead_to_rights_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            dead_to_rights_boost
        ),
    ]

