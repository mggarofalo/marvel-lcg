from . import *

# Genetic Mastery

def GetAbilities() -> Sequence['Ability']:

    def genetic_mastery_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        villain = FindMisterSinister(effect)
        if villain:
            if villain.HasTrait("AERIAL"):
                identity.TakeIndirectDamage(this, 2, effect)
            if villain.HasTrait("BRUTE"):
                Faces.ExhaustAll([identity], effect)
            if villain.HasTrait("PSIONIC"):
                this.PlaceThreatOnSchemes("MainScheme", 2, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            genetic_mastery_revealed
        ),
    ]

