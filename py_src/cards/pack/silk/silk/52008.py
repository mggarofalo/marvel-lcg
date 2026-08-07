from . import *

# Eidetic Memory

def GetAbilities() -> Sequence['Ability']:

    def eidetic_memory(effect: 'Effect', message: 'Message.WhenPlayerRevealCard') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        message.SetBeInstead(effect)

        face = effect.targets[0]
        Faces.SwapTwoCards([face, message.trigger], effect)
        message.trigger.FlipTo(effect, face_up=False) # Hack

        face.Reveal(initiator, effect)

    return [
        AbilityFactory.WhenPlayerRevealCard(
            AbilityType.Interrupt,
            "You",
            None,
            "Yes",
            eidetic_memory,
            # conditions=[
            #     lambda effect, message:
            #         message.trigger.paper.set_name in 
            #         [x.paper.set_name for x in effect.GetInitiator().GetIdentity().GetPlacedCardArea().GetAll()]
            # ]
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("TuckUnderYourIdentityCard", check_fn=lambda effect, face:
            effect.GetBindMessage(Message.WhenPlayerRevealCard).trigger.paper.set_name == face.paper.set_name
        ),
    ]

