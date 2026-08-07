from . import *

# * Wiccan: William Kaplan

def GetAbilities() -> Sequence['Ability']:

    def wiccan(effect: 'Effect', message: 'Message.AfterUnitThwartEnd') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Worlds.DiscardEncounterTopCard(effect)
        if face:
            max_num = FacesCounter.CountTotalBoostIcons([face])
            initiator.MayChooseOneAbility(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        this.DealDamage(targets, 1, effect)
                ).SetTarget(Enemy,
                    range=(1, max_num),
                    repeat_rules="Health"
                )
            )

    return [
        AbilityFactory.AfterUnitThwartEnd(
            AbilityType.Response,
            "This",
            wiccan
        ),
    ]

