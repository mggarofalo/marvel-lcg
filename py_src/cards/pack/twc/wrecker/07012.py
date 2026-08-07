from . import *

# Crowbar Toss

def GetAbilities() -> Sequence['Ability']:

    def crowbar_toss_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        wrecker = GetWrecker(effect)
        player = message.GetToPlayer()
        wrecker.DoSchemes(player, effect)

        scheme = GetSideSchemeWhoseHas(effect, least_threat=True)
        villain = scheme.FindCorrespondingVillain()
        if villain:
            villain.SetActive(effect)

    def crowbar_toss_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        wrecker = GetWrecker(effect)
        player = message.GetToPlayer()
        wrecker.DoAttackYou(player, effect)

        scheme = GetSideSchemeWhoseHas(effect, least_threat=True)
        villain = scheme.FindCorrespondingVillain()
        if villain:
            villain.SetActive(effect)

    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            crowbar_toss_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            crowbar_toss_hero
        )
    ]

