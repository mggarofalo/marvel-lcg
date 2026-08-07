from . import *

# Masters of Mayhem

def GetAbilities() -> Sequence['Ability']:

    def masters_of_mayhem(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        def action():
            player = message.GetToPlayer()
            face = Search.EncounterCard(
                effect,
                player,
                include_discard_pile=True,
                trait="MASTERS OF EVIL",
                card_type=Minion,
            )
            if face:
                face.PutIntoPlay(player, effect)

        Worlds.Enemies.AllMinionAttackTheHeroItEngagedWith(effect, trait="MASTERS OF EVIL", if_no_attack=action)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            masters_of_mayhem
        )
    ]
