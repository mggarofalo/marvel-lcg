from . import *

# Sector Scan

def GetAbilities() -> Sequence['Ability']:

    def sector_scan(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        def action():
            deck = Worlds.GetEncounterDeck(effect)
            face = deck.GetTop()
            if face:
                face.card.visible.Set(initiator)

        action()

        this.effect.RegisterTemp(
            AbilityFactory.AfterCardsMoved(
                AbilityType.Temp0,
                None,
                lambda effect, message:
                    action(),
            ),
            unregister_after_exec=False,
            until_round_end=True
        )

    return [
        AbilityFactory.ReduceCostToPlayThis(
            lambda effect:
                GetIronheartVersionNumber(effect.GetInitiator())
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            sector_scan
        ).SetPlay().SetLabel(),
    ]

