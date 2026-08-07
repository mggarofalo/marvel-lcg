from . import *

# Blind Side

def GetAbilities() -> Sequence['Ability']:

    def blind_side_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        player.ChooseAbilities(
            effect,
            ExhaustTheMilanoForChoiceAbility(effect),
            AbilityFactory.ForChoiceAbilityWithCost(
                Cost("RR"),
            ),
            AbilityFactory.ForChoiceAbility(
                "Stun the first player",
                lambda targets:
                    Faces.GiveStatus(targets, "Stunned", effect),
            ).SetTarget("FirstPlayer", canbe_stunned=True)
        )


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            blind_side_revealed
        ),
    ]

