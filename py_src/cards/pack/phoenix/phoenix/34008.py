from . import *

# Mental Paralysis

def GetAbilities() -> Sequence['Ability']:

    def mental_paralysis(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.DiscardAll([this], effect)


    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            NonEliteMinion,
        ).SetPlay(hero_form_only=True),
        AbilityFactory.EnemyCannotActivate(
            AbilityType.NonKeyword,
            "AttachedMinion"
        ),
        AbilityFactory.AfterUnitChangeForm(
            AbilityType.ForcedResponse,
            "You",
            mental_paralysis,
            to_form=AlterEgo,
        ),
    ]

