from . import *

# Guardians of the Galaxy

def GetAbilities() -> Sequence['Ability']:

    def guardians_of_the_galaxy(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(1, effect)


    return [
        AbilityFactory.CanPlayThisSupportCard(
            under_any_players_control=True
        ),
        AbilityFactory.AfterPlayerPlayedCard(
            AbilityType.Response,
            "You",
            Upgrade,
            guardians_of_the_galaxy,
            conditions=[
                lambda effect, message:
                    Faces.EachOfAre(effect.GetInitiator().GetControlCharacters(), CardFinder2("GUARDIAN")) and \
                    len(message.play_effect.targets) == 1 and \
                    Ally.IsType(message.play_effect.targets[0]),
            ],
        ),
    ]

