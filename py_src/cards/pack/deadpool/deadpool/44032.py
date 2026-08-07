from . import *

# The Merc with the Mouth

def GetAbilities() -> Sequence['Ability']:

    def the_merc_with_the_mouth(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        Faces.ExhaustAll(player.GetControlAllies(), effect)

    def you_have_not_talked_this_phase(effect: 'Effect', message: 'Message.WhenPhaseEnd') -> bool:
        return True

    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            the_merc_with_the_mouth
        ),
        AbilityFactory.WhenCardWouldReady(
            AbilityType.NonKeyword,
            Ally,
            lambda effect, message:
                message.SetBeInstead(effect),
            control_by="You"
        ),
        AbilityFactory.PlayersCannotTriggerAbility(
            "Other",
            None,
            on_which_card=PlayerCard,
            during="YourTurn"
        ),
        AbilityFactory.WhenPlayerPhaseEnd(
            AbilityType.ForcedResponse,
            DiscardThisCard,
            conditions=[you_have_not_talked_this_phase],
        )
    ]

