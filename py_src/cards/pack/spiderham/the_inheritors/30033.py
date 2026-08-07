from . import *

# * Daemos

def GetAbilities() -> Sequence['Ability']:

    def daemos_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        if Worlds.FindCardOnField(effect, CardFinder2("WEB-WARRIOR", Unit2)):
            player.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "Stun a character you control",
                    lambda targets:
                        Faces.GiveStatus(targets, "Stunned", effect)
                ).SetTarget("YouControlUnit", canbe_stunned=True)
            )


    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder2("INHERITOR", Minion),
            stalwart=1
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            daemos_revealed
        ),
    ]

