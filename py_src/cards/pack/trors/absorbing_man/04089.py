from . import *

# Omni-Morph Duplication

def GetAbilities() -> Sequence['Ability']:

    def omni_morph_duplication_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        villain = GetAbsorbingMan(effect)
        if villain.HasTrait('ICE'):
            identity = player.GetIdentity()
            Faces.ExhaustAll([identity], effect)
        if villain.HasTrait('METAL'):
            Faces.GiveStatus([villain], "Tough", effect)
            this.HealthUnits([villain], 1, effect)
        if villain.HasTrait('STONE'):
            Faces.GiveFacedownBoostCards([villain], 1, effect)
        if villain.HasTrait('WOOD'):
            player.DiscardRandomHandCards(1, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            omni_morph_duplication_revealed
        ),
    ]

