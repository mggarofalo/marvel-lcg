from . import *

# Security Cameras

def GetAbilities() -> Sequence['Ability']:

    def security_cameras_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        env = GetAlertLevel(effect)
        if env:
            Faces.RemoveTokensOn([env], 1, 'threat', effect)

        ThisCardGainSurge(effect)

    def security_cameras_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        if IfAlertLevelIsHighSide(effect):
            threat = 2
        else:
            threat = 1

        env = GetAlertLevel(effect)

        faces = player.GetControlCharacters()
        size = len(faces)

        def action(targets: Sequence['CardFace']):
            nonlocal size
            Faces.ExhaustAll(targets, effect)
            size -= len(targets)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Exhaust characters",
                action
            ).SetTarget(faces, canbe_exhaust=True, range=(0, "All")),
        )

        if env:
            Faces.PlaceTokensOn([env], size * threat, 'threat', effect)

        # for face in player.GetControlCharacters():
        #     player.ChooseAbilities(
        #         effect,
        #         AbilityFactory.ForChoiceAbility(
        #             "Exhaust that character",
        #             lambda targets:
        #                 Faces.ExhaustAll(targets, effect)
        #         ).SetTarget([face], canbe_exhaust=True),
        #         AbilityFactory.ForChoiceAbility(
        #             "Place 1 threat on Alert Level",
        #             lambda targets:
        #                 Faces.PlaceTokensOn(targets, threat, 'threat', effect)
        #         ).SetTarget([env] if env != None else [])
        #     )


    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            security_cameras_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            security_cameras_hero
        ),
    ]

