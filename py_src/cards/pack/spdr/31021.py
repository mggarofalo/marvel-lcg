from . import *

# * Spider-Ham: Peter Porker

def GetAbilities() -> Sequence['Ability']:

    def spider_ham(effect: 'Effect', message: 'Message.AfterUnitAttackEnd|Message.AfterUnitThwartEnd') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        faces = Worlds.DiscardEncounterCards( 1, effect)
        damage = FacesCounter.CountTotalBoostIcons(faces)
        this.DealDamage([this], damage, effect)


    return [
        AbilityFactory.CanPlayThisAllyCard(
        ).SetPlay(only_if_you_control_trait="WEB-WARRIOR"),
        AbilityFactory.AfterUnitAttackOrThwart(
            AbilityType.ForcedResponse,
            "This",
            spider_ham,
        ),
    ]

