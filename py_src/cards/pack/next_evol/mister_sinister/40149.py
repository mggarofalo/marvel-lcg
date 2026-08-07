from . import *

# Sinister Schemes

def GetAbilities() -> Sequence['Ability']:

    def sinister_schemes_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        villain = FindMisterSinister(effect)
        if villain:
            villain.DoSchemes(player, effect)
            if villain.HasTrait("PSIONIC"):
                identity = player.GetIdentity()
                Faces.GiveStatus([identity], "Confused", effect)


    def sinister_schemes_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        Faces.GiveStatus([identity], "Confused", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            sinister_schemes_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            sinister_schemes_boost
        ),
    ]

