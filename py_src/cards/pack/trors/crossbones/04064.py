from . import *

# * Crossbones' Machine Gun

def GetAbilities() -> Sequence['Ability']:

    def crossbones_machine_gun(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        face = effect.cost_func.Get(CostFunc.Discard).return_discarded_cards[0]
        damage = FacesCounter.CountTotalBoostIcons([face])

        Players.ForEachTargets(
            message.attacked_targets,
            lambda player:
                player.GetIdentity().TakeIndirectDamage(this, damage, effect)
        )

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Crossbones")
        ),
        AbilityFactory.WhenUnitAttackYou(
            AbilityType.ForcedInterrupt,
            CardFinder(name="Crossbones"),
            crossbones_machine_gun,
        ).SetCostFunc(CostFunc.Counter("This", 1, 'ammo'))
        .SetCostFunc(CostFunc.Discard("EncounterDeckTop"))
    ]

