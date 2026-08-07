from . import *

# * Warpath: James Proudstar

def GetAbilities() -> Sequence['Ability']:

    def warpath(effect: 'Effect', message: 'Message.AfterUnitDefendEnd') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.PlayCardsLikeInTurn(effect.targets, effect)

    def can_be_play(effect: 'Effect', face: 'CardFace') -> bool:
        initiator = effect.GetInitiator()
        if not Event.IsType(face):
            return False
        if not face.effect.Find(type=AbilityType.HeroAction):
            return False
        if not face.CanPlayBy(initiator):
            return False
        return True

    return [
        AbilityFactory.AfterUnitDefendAgainstAttack(
            AbilityType.HeroResponse,
            "This",
            warpath
        ).SetTarget(check_fn=can_be_play, from_where=["YourHandCards"]),
    ]

