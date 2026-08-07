from . import *

# Mutant Detected

def GetAbilities() -> Sequence['Ability']:

    def mutant_detected_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        def action(targets: Sequence['CardFace']):
            Worlds.Enemies.VillainAndEngagedMinionsAttackYou(effect, player)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Place the top card of your deck facedown under Operation Zero Tolerance",
                lambda targets:
                    PlaceDeckTopCardFacedownUnderOperationZeroTolerance(player, effect)
            ),
            AbilityFactory.ForChoiceAbility(
                "The villain and each minion engaged with you attacks you",
                action
            )
        )


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            mutant_detected_revealed
        ),
    ]

