from . import *

# * Sindr

def GetAbilities() -> Sequence['Ability']:

    def sindr_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        if identity.HasTrait("DEFIANT"):
            this.DoActivate(player, effect)
        if identity.HasTrait("ENTHRALLED"):
            Faces.GiveStatus([this], "Tough", effect)


    return [
        AbilityFactory.UnitAttackGainKeyword(
            "This",
            indirect_damage=True,
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            sindr_revealed
        ),
    ]

