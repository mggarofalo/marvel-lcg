from . import *

# Master Strategist

def GetAbilities() -> Sequence['Ability']:

    def master_strategist(effect: 'Effect', message: 'Message.WhenEnemyActivateAgainstYou') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        villain = this.GetBindFace().CastTo(Villain)
        value = len(Worlds.GetSideSchemes(effect))
        Faces.GiveFacedownBoostCards([villain], value, effect)
        Faces.DiscardAll([this], effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Red Skull")
        ),
        AbilityFactory.WhenEnemyActivateAgainstYou(
            AbilityType.ForcedInterrupt,
            CardFinder(name="Red Skull"),
            master_strategist
        ).LimitOncePerEvent(),
    ]

