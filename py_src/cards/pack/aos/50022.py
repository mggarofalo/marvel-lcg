from . import *

# * Grant Ward

def GetAbilities() -> Sequence['Ability']:

    def grant_ward(effect: 'Effect', message: 'Message.AfterCardRevealedEnd') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()

        if not initiator.AskSpendResources(Cost("B"), effect):
            initiator.GetIdentity().TakeDamage(this, this.attack, effect)
            Faces.RemoveAllFromGame([this], effect)

    return [
        *AbilityFactory.UnitCannotDefend(
            "This",
            None,
            cannot_trigger_defense_ability=False,
        ),
        AbilityFactory.AfterPlayerRevealCard(
            AbilityType.ForcedResponse,
            "You",
            Treachery,
            grant_ward
        )
    ]

