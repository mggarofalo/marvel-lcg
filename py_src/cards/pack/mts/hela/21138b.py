from . import *

# Odin's Torment

def GetAbilities() -> Sequence['Ability']:

    def odins_torment(effect: 'Effect', message: 'Message.WhenUnitWouldBeDefeated') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        message.SetBeInstead(effect)

        hela = Worlds.FindVillain(effect)
        if hela:
            faces = Filter.ByType(hela.components.inventory.GetDeck().Get(), Attachment)
            Faces.DiscardAll(faces, effect)

            hela.FlipTo(effect, trait="WOUNDED")


    def is_odin_attached_to_this(effect: 'Effect', message: 'Message.WhenUnitWouldBeDefeated') -> bool:
        this = effect.this.CastTo(MainScheme)
        return this.components.inventory.GetDeck().FindCard(name="Odin") != None

    return [
        AbilityFactory.WhenUnitWouldBeDefeated(
            AbilityType.ForcedInterrupt,
            Villain,
            odins_torment,
            conditions=[is_odin_attached_to_this]
        ),
    ]

