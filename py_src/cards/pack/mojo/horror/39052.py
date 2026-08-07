from . import *

# Werewolf Pack

def GetAbilities() -> Sequence['Ability']:

    def werewolf_pack_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        allies = player.GetControlAllies()
        face = player.AskChooseFace(allies, effect)
        if face:
            Faces.DefeatUnits([face], this, effect)
            this.PlaceCardHere(face, False, effect)


    def werewolf_pack(effect: 'Effect', message: 'Message.WhenUnitWouldBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        faces = this.GetPlacedCardArea().Get()
        if faces:
            message.SetBeInstead(effect)
            Faces.DiscardAll(faces, effect)
            this.HealHealth(this.sustained, effect)
        else:
            pass


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            werewolf_pack_revealed
        ),
        AbilityFactory.WhenUnitWouldBeDefeated(
            AbilityType.ForcedInterrupt,
            "This",
            werewolf_pack
        ),
    ]

