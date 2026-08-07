from . import *

# Sinister Ends

def GetAbilities() -> Sequence['Ability']:

    def sinister_ends(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        ally = Worlds.FindCardOnField(
            effect,
            name="Hope Summers",
            card_type=Ally
        )
        if ally:
            message.ReplaceTarget(ally)


    return [
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.ForcedInterrupt,
            CardFinder(name="Mister Sinister"),
            sinister_ends
        ),
    ]

