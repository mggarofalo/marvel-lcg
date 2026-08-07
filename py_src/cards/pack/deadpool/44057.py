from . import *

# * Tic-Tac-Toe

def GetAbilities() -> Sequence['Ability']:

    def tic_tac_toe(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        def to_counter_name(name: str) -> 'CardFace.COUNTER':
            name = name.replace("-", "_")
            counter_name = f"tic_tac_toe_{name}"
            assert IsCounter(counter_name)
            return counter_name

        initiator = effect.GetInitiator()
        res = effect.GetPaidResources(False)
        places: List[str] = []
        if res.HasColor("R"):
            places.extend(["3-1", "3-2", "3-3"])
        if res.HasColor("B"):
            places.extend(["2-1", "2-2", "2-3"])
        if res.HasColor("Y"):
            places.extend(["1-1", "1-2", "1-3"])

        for place in places[:]:
            counter_name = to_counter_name(place)
            if this.GetCounters(counter_name) > 0:
                places.remove(place)

        if places:
            place = initiator.AskChooseOneText(places)
            counter_name = to_counter_name(place)
            Faces.PlaceCountersOn([this], 1, counter_name, effect)
            this.HealthUnits(effect.targets, 1, effect)

            line_list = [
                ["1-1", "1-2", "1-3"],
                ["2-1", "2-2", "2-3"],
                ["3-1", "3-2", "3-3"],
                ["1-1", "2-1", "3-1"],
                ["1-2", "2-2", "3-2"],
                ["1-3", "2-3", "3-3"],
                ["1-1", "2-2", "3-3"],
                ["1-3", "2-2", "3-1"],
            ]

            for line in line_list:
                found = True
                for place in line:
                    counter_name = to_counter_name(place)
                    if this.GetCounters(counter_name) == 0:
                        found = False
                        break
                if found:
                    enemies = Worlds.GetOnFieldEnemies(effect)
                    enemy = initiator.AskChooseFace(enemies, effect)
                    if enemy:
                        places = [
                            "1-1", "1-2", "1-3",
                            "2-1", "2-2", "2-3",
                            "3-1", "3-2", "3-3",
                        ]
                        damage = 0
                        for place in places:
                            counter_name = to_counter_name(place)
                            if this.GetCounters(counter_name) > 0:
                                damage += 1
                        this.DealDamage([enemy], damage, effect)
                        Faces.DiscardAll([this], effect)
                    break


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            tic_tac_toe
        ).SetCost(Cost("1"))
        .SetTarget(Unit2, canbe_heal=True),
    ]

