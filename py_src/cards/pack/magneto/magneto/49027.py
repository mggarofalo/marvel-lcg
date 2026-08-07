from . import *

# Old Grievances

def GetAbilities() -> Sequence['Ability']:

    def old_grievances(effect: 'Effect', message: 'Message.AfterEffectResolved') -> None:
        this = effect.this.CastTo(Obligation)
        player = this.GetBindFace().GetControlByPlayer()

        faces = player.GetIdentity().GetBuff(Buff_49001a).faces

        damage = len(faces)

        player.GetIdentity().TakeDamage(this, damage, effect)


    return [
        AbilityFactory.AfterPlayerResolveAbility(
            AbilityType.ForcedResponse,
            "You",
            None,
            old_grievances,
            ability_name="Magnetic Pull",
            control_by_you=True,
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.AlterEgoAction,
        ).SetCostFunc(CostFunc.Exhaust("YouControlUnit", name="Erik Lehnsherr")),
    ]

