from cards.pack import *

def ControlHaveMutantOrXMen(effect: 'Effect') -> int:
    this = effect.this.CastTo(Ally)
    Unused(this)

    player = this.GetControlByPlayer()
    return player.GetIdentity().HasTrait("MUTANT", "X-MEN")

def ControlHaveMutantOrXMenUI(effect: 'Effect', ui: List['CardFace']) -> int:
    this = effect.this.CastTo(Ally)
    Unused(this)

    player = this.GetControlByPlayer()
    identity = player.GetIdentity()
    if identity.HasTrait("MUTANT", "X-MEN"):
        ui.append(identity)
        return 1
    return 0

