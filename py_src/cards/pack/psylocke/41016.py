from . import *

# * Lay the Trap

def GetAbilities() -> Sequence['Ability']:

    def lay_the_trap(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(PlayerSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()

        villains = Worlds.GetVillains(effect)
        face = player.AskChooseFace(villains, effect)
        if face:
            player.GetIdentity().DealDamage([face], "5*", effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            lay_the_trap,
            has_defeating_player=True
        ),
    ]

