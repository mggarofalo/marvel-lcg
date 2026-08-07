from . import *

# Get Wrecked!

def GetAbilities() -> Sequence['Ability']:

    def get_wrecked_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        scheme = GetSideSchemeWhoseHas(effect, most_threat=True)
        villain = scheme.FindCorrespondingVillain()

        if villain:
            player = message.GetToPlayer()
            villain.DoSchemes(player, effect)

    def get_wrecked_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        scheme = GetSideSchemeWhoseHas(effect, least_threat=True)
        villain = scheme.FindCorrespondingVillain()

        if villain:
            player = message.GetToPlayer()
            villain.DoAttackYou(player, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            get_wrecked_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            get_wrecked_hero
        )
    ]

