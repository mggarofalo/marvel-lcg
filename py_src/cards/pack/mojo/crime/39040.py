from . import *

# Elementary, My Dear Mojo

def GetAbilities() -> Sequence['Ability']:

    def elementary_my_dear_mojo_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        main_scheme = Worlds.FindMainScheme(this)

        def move_threats(targets: Sequence['CardFace']):
            assert main_scheme
            for target in targets:
                if SchemeSide2.IsType(target):
                    threat = target.threat
                    if target.RemoveThreatFromSchemes([target], threat, effect):
                        this.PlaceThreatOnSchemes([main_scheme], threat, effect)

        def action(targets: Sequence['CardFace']):
            face = Worlds.DiscardEncounterCardsUntil(
                effect,
                card_type=SchemeSide2
            )
            if face:
                face.Reveal(player, effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Move all threat from a side scheme to the main scheme",
                move_threats,
                condition=main_scheme != None
            ).SetTarget(SchemeSide2),
            AbilityFactory.ForChoiceAbility(
                "Discard cards from the top of the encounter deck until a side scheme is discard",
                action
            )
        )
    

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            elementary_my_dear_mojo_revealed
        ),
    ]

