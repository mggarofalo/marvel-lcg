from . import *

# Inconspicuous

def GetAbilities() -> Sequence['Ability']:

    def inconspicuous_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Discard the highest-cost card you control",
                lambda targets:
                    Faces.DiscardAll(targets, effect)
            ).SetTarget("YouControlCards", highest_cost=True),
            AbilityFactory.ForChoiceAbility(
                "Spider-Woman schemes",
                lambda targets:
                    targets[0].CastTo(Enemy).DoSchemes(player, effect)
            ).SetTarget(name="Spider-Woman", card_type=Enemy)
        )

    def inconspicuous_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        size = identity.GetStatusSize()
        this.GainBoostIcons(size, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            inconspicuous_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            inconspicuous_boost
        ),
    ]

