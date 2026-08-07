from . import *

# Chaos In the Prison

def GetAbilities() -> Sequence['Ability']:

    def chaos_in_the_prison(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        upgrades = player.GetControlUpgrade()

        if len(upgrades) == 0:
            this.GainSurge(1, effect)
        else:
            villain = Worlds.FindVillain(effect)
            schemes: List[CardFace] = []
            if villain:
                scheme = villain.FindCorrespondingScheme()
                if scheme:
                    schemes = [scheme]

            def place_threat(targets: Sequence['CardFace']):
                size = len(player.GetIdentity().GetAttachedUpgrades())
                this.PlaceThreatOnSchemes(targets, size, effect)

            player.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "Discard an upgrade you control",
                    lambda targets:
                        Faces.DiscardAll(targets, effect)
                ).SetTarget(upgrades, canbe_discard=True),
                AbilityFactory.ForChoiceAbility(
                    "Place 1 threat on the active villain's side scheme for each upgrade you control",
                    place_threat,
                ).SetTarget(schemes),
            )

    def chaos_in_the_prison_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        player = message.GetToPlayer()
        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                # "discard an upgrade you control",
                "",
                lambda targets:
                    Faces.DiscardAll(targets, effect)
            ).SetTarget(Upgrade, from_where=["YouControlCards"], canbe_discard=True)
        )

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            chaos_in_the_prison
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            chaos_in_the_prison_boost,
            is_undefended_attack=True,
        )
    ]

