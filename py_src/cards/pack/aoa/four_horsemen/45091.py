from . import *

# Metal Wings

def GetAbilities() -> Sequence['Ability']:

    def metal_wings(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.attacker.GetControlByPlayer()

        villain = message.attacked.CastTo(Villain)
        Faces.ResolveAbility([villain], player, AbilityType.ForcedResponse, effect)
        Faces.DiscardAll([this], effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Death"),
            when_attach_operation=lambda face, effect:
                face.CastTo(Villain).SetActive(effect)
        ),
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Death"),
            retaliate=1,
            considered_at_least_hp=1,
        ),
        AbilityFactory.AfterUnitAttackUnit(
            AbilityType.Response,
            Identity,
            CardFinder(name="Death"),
            metal_wings
        ),
    ]

