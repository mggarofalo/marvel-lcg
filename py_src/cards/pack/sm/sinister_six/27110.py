from . import *

# Robotic Enhancements

def GetAbilities() -> Sequence['Ability']:

    def robotic_enhancements_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        if Worlds.FindVillain(effect, "Doctor Octopus"):
            player.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        Faces.GiveStatus(targets, "Confused", effect)
                ).SetTarget("YouControlUnit", canbe_confused=True)
            )
        else:
            PutSetasideVillainIntoPlay("Doctor Octopus", effect)

        if Worlds.FindVillain(effect, "Scorpion"):
            player.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        Faces.GiveStatus(targets, "Stunned", effect)
                ).SetTarget("YouControlUnit", canbe_stunned=True)
            )
        else:
            PutSetasideVillainIntoPlay("Scorpion", effect)


    return [
        AbilityFactory.IfExpertModeThisGainKeyword(
            incite=1
        ),
        AbilityFactory.IfExpertModeThisCannotBeCanceled(),
        AbilityFactory.WhenThisRevealed(
            None,
            robotic_enhancements_revealed
        ),
    ]

