from . import *

# * Sandman

def GetAbilities() -> Sequence['Ability']:

    def sandman(effect: 'Effect', message: 'Message.AfterUnitTookDamage|Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        Worlds.DiscardEncounterCards(7, effect)


    return [
        AbilityFactory.AfterUnitTookDamage(
            AbilityType.ForcedResponse,
            "This",
            sandman,
            is_from_attack=True,
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            sandman
        ),
    ]

