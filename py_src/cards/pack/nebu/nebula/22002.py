from . import *

# * Gamora

def GetAbilities() -> Sequence['Ability']:

    def gamora(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        for face in effect.targets:
            face.ResolveSpecialAbility(initiator, effect)

    return [
        AbilityFactory.AfterPlayerPlayedCard(
            AbilityType.Response,
            "You",
            "This",
            gamora
        ).SetTarget(Upgrade, trait="TECHNIQUE", from_where=["YouControlCards"]),
    ]

