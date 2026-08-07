from . import *

# Discovered

def GetAbilities() -> Sequence['Ability']:

    def discovered(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        player.form.ChangeSuitForm(effect, "Assault")

        upgrade = player.form.FindSuitForm()
        damage = 0
        if upgrade:
            damage = upgrade.GetTokens('threat')
            if damage == 0:
                ThisCardGainSurge(effect)
            else:
                identity = player.GetIdentity()
                player.ChooseAbilities(
                    effect,
                    AbilityFactory.ForChoiceAbility(
                        f"take {damage} damage for each threat on your suit form upgrade",
                        lambda targets:
                            identity.TakeDamage(this, damage, effect),
                    ).SetTarget("YourIdentity"),
                    AbilityFactory.ForChoiceAbility(
                        "Remove each threat from your suit form upgrade",
                        lambda targets:
                            Faces.RemoveTokensOn(targets, "All", 'threat', effect),
                    ).SetTarget("YourSuitFormUpgrade")
                )

        Faces.DiscardAll([this], effect)

    return [
        AbilityFactory.WhenThisObligationRevealed(
            discovered
        ),
    ]

