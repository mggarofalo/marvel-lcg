from . import *

# * Mystique

def GetAbilities() -> Sequence['Ability']:

    def mystique_atk(effect: 'Effect', ui: List['CardFace']) -> Tuple[int, bool]:
        this = effect.this.CastTo(Minion)
        Unused(this)

        villain = Worlds.FindVillain(effect)
        if villain:
            attack = villain.attack
            ui.append(villain)
        else:
            attack = 0
        return attack, attack != this.attack

    def mystique_sch(effect: 'Effect', ui: List['CardFace']) -> Tuple[int, bool]:
        this = effect.this.CastTo(Minion)
        Unused(this)

        villain = Worlds.FindVillain(effect)
        if villain:
            scheme = villain.scheme
            ui.append(villain)
        else:
            scheme = 0
        return scheme, scheme != this.scheme

    return [
        AbilityFactory.PlayersCannotAttackWhile(
            "AnyPlayer",
            Villain,
        ),
        AbilityFactory.ThisSetKeyword(
            mystique_atk,
            attack=1,
            change_on_event=OnEvent.CardKeyword("Character")
        ),
        AbilityFactory.ThisSetKeyword(
            mystique_sch,
            scheme=1,
            change_on_event=OnEvent.CardKeyword("Character")
        ),
    ]

