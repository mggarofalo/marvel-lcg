from . import *

# Sneak Attack

def GetAbilities() -> Sequence['Ability']:

    def sneak_attack(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        for ally in effect.targets:
            ally.PutIntoPlay(initiator, effect)

        def action():
            allies: List['CardFace'] = []
            for ally in effect.targets:
                if ally.IsInPlay():
                    allies.append(ally)
            Faces.DiscardAll(allies, effect)

        RunAt.TheEndOfThePhase(effect, action)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            sneak_attack
        ).SetPlay().SetLabel()
        .SetTarget(Ally, share_trait_with_your_identity=True, from_where=["YourHandCards"]),
    ]

