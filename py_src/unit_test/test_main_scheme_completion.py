"""Ownership tests for losing when a main scheme completes (MARVEL-124)."""

import unittest

from tools.determinism.headless import _initialize_engine


class TestPrintedCompletionLoss(unittest.TestCase):

    @classmethod
    def setUpClass(cls):
        _initialize_engine()

    def test_standard_sentence_ignores_bold_tag_spacing(self):
        from game.card.face.card_type import MainScheme

        self.assertTrue(MainScheme.PrintsPlayersLoseOnCompletion(
            "<b> If this scheme is completed, the players lose the game. </b>"
        ))

    def test_a_different_loss_condition_is_not_the_standard_sentence(self):
        from game.card.face.card_type import MainScheme

        self.assertFalse(MainScheme.PrintsPlayersLoseOnCompletion(
            "<b>If this stage is completed or there are no allies in play, "
            "the players lose the game.</b>"
        ))

    def test_every_known_masked_or_duplicate_scheme_has_one_loss_owner(self):
        from cards.database import CardsDB
        from game.card.factory import CardFactory
        from game.message import Message

        class StubWorld:
            def GetPlayerNumIcon(self):
                return 1

        # Secret Rendezvous is the original mutation case. Odin's Torment has
        # whitespace inside its bold tags. Balance the Scales and All Hail King
        # Loki have incomplete imported text. Uncontrollable Power used to
        # register the same ability both generically and in its script.
        for card_id in ("01117b", "21138b", "21115b", "21165b", "40166b"):
            with self.subTest(card_id=card_id):
                face = CardFactory.CreateFace(
                    CardsDB.FindCardPaper(card_id), StubWorld())
                owners = [ability for ability in face.ability.abilities
                          if ability.when == Message.WhenMainSchemeStageCompleted]
                self.assertEqual(len(owners), 1)


if __name__ == "__main__":
    unittest.main()
