from . import *

# Sabretooth Strikes

def GetAbilities() -> Sequence['Ability']:

    def sabretooth_strikes_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        ally = Worlds.FindCardOnField(
            effect,
            ROBERT_KELLY_FINDER
        )
        if ally:
            player.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "Deal 1 damage to Robert Kelly",
                    lambda targets:
                        this.DealDamage([ally], 1, effect)
                ),
                AbilityFactory.ForChoiceAbility(
                    "Exhaust your hero to prevent this",
                    lambda targets:
                        Faces.ExhaustAll(targets, effect)
                ).SetTarget(
                    [identity],
                    finder=CardFinder(card_type=Hero, canbe_exhaust=True)),
            )

    def sabretooth_strikes_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        def action(ally: 'CardFace'):
            this.PlaceThreatOnSchemes("MainScheme", 2, effect)
        message.would_atk_message.IfThisAttackDefeats(Ally, action, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            sabretooth_strikes_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            sabretooth_strikes_boost,
            during_attack=True
        ),
    ]

