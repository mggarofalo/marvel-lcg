from . import *

# Rogue Vessel

def GetAbilities() -> Sequence['Ability']:

    def rogue_vessel(effect: 'Effect', message: 'Message.WhenPhaseEnd') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        identities = Worlds.GetPlayersIdentities(effect)
        this.DealDamage(identities, 1, effect)


    return [
        AbilityFactory.WhenVillainPhaseEnd(
            AbilityType.ForcedInterrupt,
            rogue_vessel,
        ),
        ExhaustTheMilano(
            None,
            lambda effect, message:
                Faces.DiscardAll([effect.this], effect),
        ).SetCost(Cost("2")),
    ]

