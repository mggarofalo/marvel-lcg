from . import *

# Sinister Synchronization

def GetAbilities() -> Sequence['Ability']:

    def sinister_synchronization_special(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        villain = ChooseSetAsideVillainAtRandom(effect)
        if villain:
            villain.PutIntoPlay("FirstPlayer", effect)
            villain.SetActive(effect)
        if Worlds.IsExpert(effect):
            scheme = GetLightAtTheEnd(effect)
            this.PlaceThreatOnSchemes([scheme], 2, effect)


    def sinister_synchronization(effect: 'Effect', message: 'Message.AfterResolveVillainPhaseStep') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        first_player = Worlds.GetFirstPlayer(effect)
        this.ResolveSpecialAbility(first_player, effect, name="Ambush!")


    return [
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            sinister_synchronization_special
        ).SetName("Ambush!"),
        AbilityFactory.AfterResolveVillainPhaseStep(
            AbilityType.ForcedInterrupt,
            1,
            sinister_synchronization,
            conditions=[
                lambda effect, message:
                    [x for x in Worlds.GetVillains(effect) if x.IsActive()] == []
            ]
        ),
    ]

