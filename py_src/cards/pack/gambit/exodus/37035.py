from . import *

# Acolyte Frenzy

def GetAbilities() -> Sequence['Ability']:

    def acolyte_frenzy_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        if not player.GetEngagedMinions(CardFinder2("ACOLYTE")):
            ThisCardGainSurge(effect)
        else:
            Worlds.Enemies.EngagedMinionActivateAgainstYou(effect, player, trait="ACOLYTE")

    def acolyte_frenzy_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        Faces.GiveStatus([identity], "Stunned", effect)
        Faces.GiveStatus([identity], "Confused", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            acolyte_frenzy_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            acolyte_frenzy_boost
        ),
    ]

