from . import *

# * Slingshot: Yo-Yo Rodriguez

def GetAbilities() -> Sequence['Ability']:

    def slingshot(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        def action2(player: 'Player'):
            this.PutIntoPlay(player, effect, under_control=True)

        Players.ForEachTargets(effect.targets, action2)

        def action():
            if this.IsInPlay():
                initiator = effect.GetInitiator()
                Faces.ReturnToHand([this], initiator, effect)

        RunAt.TheEndOfThePhase(
            effect,
            action
        )

    return [
        AbilityFactory.CanPlayThisAllyCard(),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            slingshot,
        ).SetCost(Cost("Y"))
        .SetTarget("Players")
        .CanWorkOnlyInHand(),
    ]

