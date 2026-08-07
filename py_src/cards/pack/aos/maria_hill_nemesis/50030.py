from . import *

# * Controller

def GetAbilities() -> Sequence['Ability']:

    def controller(effect: 'Effect', message: 'Message.AfterEnemyActivationEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "",
                lambda targets:
                    Faces.RemoveCountersOn(targets, 1, 'all-purpose', effect)
            ).SetTarget(Support, has_counter='all-purpose')
        )

        face = Worlds.FindCardOnField(
            effect,
            name="Controlled Innocents"
        )
        if face:
            Worlds.Enemies.PutYouDeckTopCardAsFacedownMinion(player, "Controlled Minion", effect)


    return [
        AbilityFactory.AfterEnemyActivatesAgainstYou(
            AbilityType.ForcedResponse,
            "This",
            controller
        ),
    ]

