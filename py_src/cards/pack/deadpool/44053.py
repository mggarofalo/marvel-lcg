from . import *

# * Blackout

def GetAbilities() -> Sequence['Ability']:

    def blackout(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        scheme = effect.targets[0].CastTo(Scheme2)

        res = effect.GetPaidResources()

        colors: List[str] = []
        if res.HasColor("G"):
            colors = ["R", "B", "Y"]
        else:
            if res.HasColor("R"):
                colors.append("R")
            if res.HasColor("B"):
                colors.append("B")
            if res.HasColor("Y"):
                colors.append("Y")

        player = effect.GetInitiator()
        color = player.AskChooseOneText(colors)

        name =  {
            "Y": 'blackout_y',
            "R": 'blackout_r',
            "B": 'blackout_b',
        }[color]

        assert IsCounter(name)

        if this.GetCounters(name) < 2:
            scheme.RemoveThreatFromSchemes([scheme], 1, effect)
            Faces.PlaceCountersOn([this], 1, name, effect)

            def check_all_filled():
                for counter in [
                    'blackout_y',
                    'blackout_b',
                    'blackout_r'
                ]:
                    assert IsCounter(counter)
                    if this.GetCounters(counter) < 2:
                        return False
                return True

            if check_all_filled():
                Faces.DiscardAll([this], effect)
                villain = Worlds.FindVillain(effect)
                if villain:
                    Faces.GiveStatus([villain], "Confused", effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            blackout
        ).SetCost(Cost("1"))
        .SetTarget(Scheme2),
    ]

