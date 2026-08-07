from . import *

# The Vulture's Plans

def GetAbilities() -> Sequence['Ability']:

    def the_vultures_plans(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        def action(player: 'Player'):
            resource = Resources("0")
            faces = player.DiscardRandomHandCards(1, effect)
            for face in faces:
                res = FacesCounter.GetPrintedResources([face])
                resource += res
            return resource

        resource = Players.ForEachPlayer(effect, action, Resources("0"))

        scheme = Worlds.FindMainScheme(this)
        if scheme:
            this.PlaceThreatOnSchemes([scheme], resource.GetResourceIconTypes(), effect)
        pass

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            the_vultures_plans
        )
    ]
