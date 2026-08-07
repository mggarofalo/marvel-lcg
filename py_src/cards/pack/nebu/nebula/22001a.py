from . import *

# * Nebula

def GetAbilities() -> Sequence['Ability']:

    def nebula(effect: 'Effect', message: 'Message.AfterPlayerTurnBegin') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        initiator = effect.GetInitiator()
        resolved: List['CardFace'] = []
        for face in effect.targets:
            if face.ResolveSpecialAbility(initiator, effect):
                resolved.append(face)
        Faces.DiscardAll(resolved, effect)

    return [
        AbilityFactory.AfterPlayerTurnBegin(
            AbilityType.ForcedResponse,
            "You",
            nebula
        ).SetName("Combat Protocols")
        .SetTarget(Upgrade, trait="TECHNIQUE", from_where=["YouControlCards"], range="All"),
    ]

