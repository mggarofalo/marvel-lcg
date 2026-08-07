from . import *

# Prime Real Estate

def GetAbilities() -> Sequence['Ability']:

    def prime_real_estate(effect: 'Effect', message: 'Message.WhenCardSetup') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        def action(player: 'Player'):

            SetupCards.AttachTo(
                effect,
                attach_to=player.GetIdentity(),
                finder=CardFinder(name="Hypnotic Gaze"),
                include_in_play=False, # HACK
                choose="Random"
            )

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenCardSetup(
            "This",
            prime_real_estate
        ),
    ]

