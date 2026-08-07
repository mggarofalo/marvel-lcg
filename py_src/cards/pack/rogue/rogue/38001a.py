from . import *

# * Rogue

def GetAbilities() -> Sequence['Ability']:

    def rogue(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        initiator = effect.GetInitiator()
        touched = FindTouched(effect)
        if touched:
            target = effect.targets[0]
            touched.AttachTo2(target, effect)
            identity = initiator.GetIdentity()
            identity.GainUntilRoundEnd(
                effect,
                trait=target.traits
            )

    def rogue2(effect: 'Effect', message: 'Message.AfterPhaseBegin') -> None:
        touched = FindTouched(effect)
        if touched:
            Faces.SetAside([touched], effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            rogue
        ).SetName("Skin Contact")
        .SetTarget(Unit2, not_with_attach=CardFinder(name="Touched"))
        .LimitOncePerRound(),
        AbilityFactory.AfterPhaseBegin(
            AbilityType.ForcedResponse,
            "Player",
            rogue2
        ),
    ]

