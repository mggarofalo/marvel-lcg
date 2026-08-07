from . import *

# * Crossfire

def GetAbilities() -> Sequence['Ability']:

    def crossfire(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        unit = Filter.One(Worlds.GetOnFieldFriendlyCharacters(effect), effect, fewest_remaining_hp=True)
        if unit:
            message.ReplaceTarget(unit)
        message.GainOverKill(effect)
        message.GainRanged(effect)


    return [
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.ForcedInterrupt,
            "This",
            crossfire
        ),
    ]

