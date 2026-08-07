from . import *

# High-Tech Armament

def GetAbilities() -> Sequence['Ability']:

    def high_tech_armament(effect: 'Effect', message: 'Message.AfterCardAttachTo') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = Worlds.GetCurrentPlayer(effect)
        message.to_face.CastTo(Enemy).DoActivate(player, effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Crossbones"),
            if_cannot_attach_to=Villain,
        ),
        AbilityFactory.AfterCardAttachTo(
            AbilityType.ForcedResponse,
            "This",
            Enemy,
            high_tech_armament
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCostFunc(CostFunc.Exhaust("YouControlUnit")),
    ]

