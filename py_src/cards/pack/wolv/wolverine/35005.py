from . import *

# Berserker Frenzy

def GetAbilities() -> Sequence['Ability']:

    def berserker_frenzy_discard(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.DiscardAll([this], effect)

    def berserker_frenzy(effect: 'Effect', message: 'Message.AfterUnitTookDamage') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(1, effect)

    return [
        AbilityFactory.AfterUnitTookDamage(
            AbilityType.HeroResponse,
            CardFinder(name="Wolverine"),
            berserker_frenzy,
            is_from_attack=True,
            who_dealt_damage=Enemy,
        ),
        AbilityFactory.AfterUnitChangeForm(
            AbilityType.ForcedResponse,
            "You",
            berserker_frenzy_discard,
            to_form=AlterEgo,
        )
    ]

