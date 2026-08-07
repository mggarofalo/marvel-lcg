from . import *

# Grasping Tendrils

def GetAbilities() -> Sequence['Ability']:

    def grasping_tendrils(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        message.CancelThisAttack(effect)
        if effect.GetPaidResources().OnlyHasColor("R"):
            villains = Worlds.GetVillains(effect)
            villain = initiator.AskChooseFace(villains, effect)
            if villain:
                Faces.GiveStatus([villain], "Stunned", effect)


    return [
        AbilityFactory.WhenUnitInitiatesAttackAgainst(
            AbilityType.HeroInterrupt,
            Villain,
            "You",
            grasping_tendrils
        ).SetPlay().SetLabel('defense'),
    ]

