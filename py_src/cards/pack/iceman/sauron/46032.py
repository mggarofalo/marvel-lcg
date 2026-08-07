from . import *

# The Eye of Sauron

def GetAbilities() -> Sequence['Ability']:

    def the_eye_of_sauron_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        if Worlds.FindCardOnField(
            effect,
            name="Sauron"
        ):
            value = 3
        else:
            value = 2
        faces = player.DiscardDeckTopCards(value, effect)
        y = FacesCounter.GetPrintedResourcesCount(faces, "Y")
        for _ in range(y):
            this.PlaceThreatOnSchemes("MainScheme", 1, effect)
        b = FacesCounter.GetPrintedResourcesCount(faces, "B")
        for _ in range(b):
            player.DiscardHandCards((1, 1), effect)
        r = FacesCounter.GetPrintedResourcesCount(faces, "R")
        for _ in range(r):
            identity = player.GetIdentity()
            this.DealDamage([identity], 1, effect)
        g = FacesCounter.GetPrintedResourcesCount(faces, "G")
        for _ in range(g):
            player.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        Faces.ExhaustAll(targets, effect)
                ).SetTarget("YouControlUnit", canbe_exhaust=True)
            )


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            the_eye_of_sauron_revealed
        ),
    ]

