from . import *

# Superior Tactics

def GetAbilities() -> Sequence['Ability']:

    def superior_tactics_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)


        villain = Worlds.FindVillain(effect)
        if villain:
            if IsControlPowerStone(villain):
                this.PlaceThreatOnSchemes([this], "1*", effect)
            else:
                face = GetPowerStone(effect)
                if face:
                    face.AttachTo2(villain, effect)


    return [
        AbilityFactory.WhenCardWouldAttachTo(
            AbilityType.NonKeyword,
            CardFinder(name="Power Stone"),
            None,
            lambda effect, message:
                message.SetBeInstead(effect),
            conditions=[
                lambda effect, message:
                    message.to_face != Worlds.FindVillain(effect)
            ],
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            superior_tactics_revealed
        ),
    ]

