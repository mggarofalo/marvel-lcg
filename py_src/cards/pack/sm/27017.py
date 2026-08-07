from . import *

# * Spider-Man: Hobie Brown

def GetAbilities() -> Sequence['Ability']:

    def spider_man(effect: 'Effect', message: 'Message.WhenCardLeavePlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        faces = Worlds.DiscardEncounterCards(3, effect)
        damage = FacesCounter.CountTotalBoostIcons(faces)
        this.DealDamage(effect.targets, damage, effect)

    return [
        AbilityFactory.CanPlayThisAllyCard(
        ).SetPlay(only_if_you_control_trait="WEB-WARRIOR"),
        AbilityFactory.WhenCardLeavePlay(
            AbilityType.Interrupt,
            "This",
            spider_man
        ).SetTarget(Villain),
    ]

