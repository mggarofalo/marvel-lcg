from . import *

# * Silk

def GetAbilities() -> Sequence['Ability']:

    def silk(effect: 'Effect', message: 'Message.AfterUnitDefeatedUnit|Message.AfterUnitDefeatedScheme|Message.AfterCardRevealed') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        this.TuckCardUnderHere(effect.targets, effect)


    return [
        IfThereAreMoreThan4TuckedCardsHere(),
        AbilityFactory.AfterUnitDefeatedUnit(
            AbilityType.Response,
            "You",
            Minion,
            silk,
            conditions=[
                lambda effect, message:
                    message.target.card.area.flags.is_encounter_discard_pile
            ]
        ).SetTarget("Targets"),
        AbilityFactory.AfterUnitDefeatedScheme(
            AbilityType.Response,
            "You",
            SchemeSide2,
            silk,
            conditions=[
                lambda effect, message:
                    message.scheme.card.area.flags.is_encounter_discard_pile
            ]
        ).SetTarget("Targets"),
        AbilityFactory.AfterYouResolveTreachery(
            AbilityType.Response,
            silk,
            conditions=[
                lambda effect, message:
                    message.trigger.card.area.flags.is_encounter_discard_pile
            ]
        ).SetTarget("Trigger")
    ]

