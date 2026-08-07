from . import *

# Armored Unibike

def GetAbilities() -> Sequence['Ability']:

    def armored_unibike(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetAgainstPlayer()
        ResolveSpecialAbilityOnTheSETTINGEnvironment(player, effect)

    return [
        AbilityFactory.ThisMinionEngagePlayer(
            PlayerFinder(
                with_attach=CardFinder(name="Escaped Mutant", card_type=Attachment)
            )
        ),
        AbilityFactory.AfterUnitAttackEnd(
            AbilityType.ForcedResponse,
            "This",
            armored_unibike
        ),
    ]

