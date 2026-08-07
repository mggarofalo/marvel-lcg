from . import *

# * Mister Hyde

def GetAbilities() -> Sequence['Ability']:

    def mister_hyde_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        face = Worlds.FindCardOnField(
            effect,
            name="Calvin Zabo",
            card_type=Minion
        )
        if face:
            player = face.GetEngagedPlayer()
            if Faces.DiscardAll([face], effect):
                this.PutIntoPlay(player, effect)
                Faces.GiveStatus([this], "Tough", effect)
                this.DealDamage(Worlds.GetOnFieldHeroes(effect) + Worlds.GetOnFieldAllies(effect), 1, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            mister_hyde_revealed
        ),
    ]

