from . import *

# All Systems Go!

def GetAbilities() -> Sequence['Ability']:

    def all_systems_go(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        def action():
            face = Search.PlayerCard(
                effect,
                initiator,
                include_player_deck=True,
                include_discard_pile=True,
                card_type=Upgrade,
                trait="INTERFACE",
            )
            if face:
                Faces.AddToHand([face], initiator, effect)

        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Ready each [[Interface]] upgrade you control",
                lambda targets:
                    Faces.ReadyAll(targets, effect)
            ).SetTarget(Upgrade, trait="INTERFACE", from_where=["YouControlCards"], canbe_ready=True, range="All"),
            AbilityFactory.ForChoiceAbility(
                "Search your deck and discard pile for an [[Interface]] upgrade and add it to your hand. (Shuffle.)",
                lambda targets:
                    action()
            )
        )

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            all_systems_go
        ).SetPlay().SetLabel(),
    ]

