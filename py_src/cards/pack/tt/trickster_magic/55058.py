from . import *

# * Whirlwind

def GetAbilities() -> Sequence['Ability']:

    def whirlwind(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetAgainstPlayer()
        if player:
            identity = player.GetIdentity()
            faces = Worlds.GetSideSchemes(effect)
            identity.TakeIndirectDamage(this, len(faces), effect)

    return [
        AbilityFactory.AfterUnitAttackYou(
            AbilityType.ForcedResponse,
            "This",
            whirlwind
        ),
        ThePlayerWhoDefeatedThisMinionPutsTheSetAsideAllyIntoPlayUnderTheirControl("Whirlwind"),
    ]

