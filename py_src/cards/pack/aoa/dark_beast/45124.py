from . import *

# Cruel Experiment

def GetAbilities() -> Sequence['Ability']:

    def cruel_experiment(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()

        minion = Worlds.DiscardEncounterCardsUntil(
            effect,
            card_type=Minion
        )
        if minion:
            minion.Reveal(player, effect)
            this.AttachTo2(minion, effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            "Character",
            health=2,
            guard=1,
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            cruel_experiment,
        ),
    ]

