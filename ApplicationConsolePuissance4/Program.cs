using System;
using System.Collections.Generic;

namespace ApplicationConsolePuissance4
{
	// Etat possible d'une case du plateau
	enum Pion
	{
		VIDE = 0,
		JOUEUR1 = 1,
		JOUEUR2 = 2
	}

	// Niveau de difficulte de l'IA
	enum Difficulte
	{
		FACILE,
		MOYEN,
		DIFFICILE
	}

	// Represente le plateau de jeu
	// Gere la grille, le placement des pions et la detection de victoire
	class Plateau
	{
		private Pion[,] grille;
		private int lignes;
		private int colonnes;
		private int aligner;

		public Plateau(int lignes, int colonnes, int aligner)
		{
			this.lignes = lignes;
			this.colonnes = colonnes;
			this.aligner = aligner;
			this.grille = new Pion[lignes, colonnes];
			Reinitialiser();
		}

		public int GetLignes() { return lignes; }
		public int GetColonnes() { return colonnes; }
		public int GetAligner() { return aligner; }
		public Pion GetCase(int l, int c) { return grille[l, c]; }

		// Remet toutes les cases a VIDE
		public void Reinitialiser()
		{
			for (int l = 0; l < lignes; l++)
				for (int c = 0; c < colonnes; c++)
					grille[l, c] = Pion.VIDE;
		}

		// Retourne true si la colonne peut encore recevoir un pion
		public bool ColonneValide(int c)
		{
			if (c < 0 || c >= colonnes) return false;
			return grille[0, c] == Pion.VIDE;
		}

		// Retourne la ligne disponible la plus basse dans la colonne (gravite)
		public int LigneDisponible(int c)
		{
			for (int l = lignes - 1; l >= 0; l--)
				if (grille[l, c] == Pion.VIDE) return l;
			return -1;
		}

		// Place un pion dans la colonne et retourne la ligne utilisee
		public int Poser(int col, Pion p)
		{
			int lig = LigneDisponible(col);
			grille[lig, col] = p;
			return lig;
		}

		// Retire le pion le plus haut de la colonne
		// Utilise uniquement par l'IA pour annuler un coup simule
		public void Retirer(int col)
		{
			for (int l = 0; l < lignes; l++)
			{
				if (grille[l, col] != Pion.VIDE)
				{
					grille[l, col] = Pion.VIDE;
					return;
				}
			}
		}

		// Verifie si le joueur p a aligne le nombre requis de pions
		// Teste les 4 directions : horizontal, vertical, diagonales
		public bool AGagne(Pion p)
		{
			for (int l = 0; l < lignes; l++)
				for (int c = 0; c <= colonnes - aligner; c++)
					if (Alignement(l, c, 0, 1, p)) return true;

			for (int l = 0; l <= lignes - aligner; l++)
				for (int c = 0; c < colonnes; c++)
					if (Alignement(l, c, 1, 0, p)) return true;

			for (int l = 0; l <= lignes - aligner; l++)
				for (int c = 0; c <= colonnes - aligner; c++)
					if (Alignement(l, c, 1, 1, p)) return true;

			for (int l = aligner - 1; l < lignes; l++)
				for (int c = 0; c <= colonnes - aligner; c++)
					if (Alignement(l, c, -1, 1, p)) return true;

			return false;
		}

		// Retourne true si toutes les cases sont occupees (match nul)
		public bool EstPleine()
		{
			for (int c = 0; c < colonnes; c++)
				if (ColonneValide(c)) return false;
			return true;
		}

		// Affiche la grille dans la console
		public void Afficher()
		{
			Console.WriteLine();
			for (int l = 0; l < lignes; l++)
			{
				Console.Write("| ");
				for (int c = 0; c < colonnes; c++)
				{
					char sym = '.';
					if (grille[l, c] == Pion.JOUEUR1) sym = 'X';
					if (grille[l, c] == Pion.JOUEUR2) sym = 'O';
					Console.Write(sym + " ");
				}
				Console.WriteLine("|");
			}
			Console.Write("+");
			for (int c = 0; c < colonnes; c++) Console.Write("--");
			Console.WriteLine("-+");
			Console.Write("  ");
			for (int c = 0; c < colonnes; c++) Console.Write((c + 1) + " ");
			Console.WriteLine("\n");
		}

		// Verifie si aligner pions identiques se suivent a partir de (l,c)
		// dans la direction (dl, dc)
		private bool Alignement(int l, int c, int dl, int dc, Pion p)
		{
			for (int k = 0; k < aligner; k++)
				if (grille[l + k * dl, c + k * dc] != p) return false;
			return true;
		}
	}

	// Classe abstraite : definit le contrat commun a tout joueur
	// Chaque sous-classe doit implementer ChoisirColonne()
	abstract class Joueur
	{
		protected string nom;
		protected Pion pion;

		public Joueur(string nom, Pion pion)
		{
			this.nom = nom;
			this.pion = pion;
		}

		public string GetNom() { return nom; }
		public Pion GetPion() { return pion; }

		public abstract int ChoisirColonne(Plateau plateau);
	}

	// Joueur humain : choisit sa colonne via la saisie clavier
	class JoueurHumain : Joueur
	{
		public JoueurHumain(string nom, Pion pion) : base(nom, pion) { }

		public override int ChoisirColonne(Plateau plateau)
		{
			int col = -1;
			while (true)
			{
				string symbole = (pion == Pion.JOUEUR1) ? "X" : "O";
				Console.Write(nom + " (" + symbole + "), choisissez une colonne (1-"
							  + plateau.GetColonnes() + ") : ");

				string saisie = Console.ReadLine();
				bool ok = int.TryParse(saisie, out col);
				col--;

				if (ok && plateau.ColonneValide(col))
					return col;

				Console.WriteLine("Colonne invalide ou pleine, reessayez.");
			}
		}
	}

	// Joueur artificiel : utilise l'algorithme Minimax avec elagage Alpha-Beta
	// Trois niveaux : FACILE (coups aleatoires), MOYEN (mixte), DIFFICILE (Minimax pur)
	class JoueurIA : Joueur
	{
		private Difficulte diff;
		private int profondeur;
		private Pion pionAdversaire;
		private Random rng;

		public JoueurIA(string nom, Pion pion, Difficulte diff) : base(nom, pion)
		{
			this.diff = diff;
			this.rng = new Random();
			this.pionAdversaire = (pion == Pion.JOUEUR1) ? Pion.JOUEUR2 : Pion.JOUEUR1;

			if (diff == Difficulte.FACILE) profondeur = 1;
			else if (diff == Difficulte.MOYEN) profondeur = 3;
			else profondeur = 6;
		}

		public override int ChoisirColonne(Plateau plateau)
		{
			Console.WriteLine(nom + " reflechit...");

			List<int> colsValides = new List<int>();
			for (int c = 0; c < plateau.GetColonnes(); c++)
				if (plateau.ColonneValide(c))
					colsValides.Add(c);

			if (colsValides.Count == 0) return -1;

			// Coup aleatoire selon le niveau de difficulte
			int seuil = 0;
			if (diff == Difficulte.FACILE) seuil = 7;
			if (diff == Difficulte.MOYEN) seuil = 3;

			if (seuil > 0 && rng.Next(10) < seuil)
				return colsValides[rng.Next(colsValides.Count)];

			// Minimax : on teste chaque colonne et on garde le meilleur score
			int meilleurCol = colsValides[0];
			int meilleurScore = int.MinValue;

			foreach (int c in colsValides)
			{
				plateau.Poser(c, pion);
				int score = Minimax(plateau, profondeur - 1, false, int.MinValue, int.MaxValue);
				plateau.Retirer(c);

				if (score > meilleurScore)
				{
					meilleurScore = score;
					meilleurCol = c;
				}
			}

			Console.WriteLine(nom + " joue en colonne " + (meilleurCol + 1));
			return meilleurCol;
		}

		// Algorithme Minimax avec elagage Alpha-Beta
		// estMax = true  tour de l'IA (on maximise)
		// estMax = false tour de l'adversaire (on minimise)
		private int Minimax(Plateau plateau, int prof, bool estMax, int alpha, int beta)
		{
			if (plateau.AGagne(pion)) return 1000000 + prof;
			if (plateau.AGagne(pionAdversaire)) return -1000000 - prof;
			if (plateau.EstPleine()) return 0;
			if (prof == 0) return Heuristique(plateau);

			if (estMax)
			{
				int meilleur = int.MinValue;
				for (int c = 0; c < plateau.GetColonnes(); c++)
				{
					if (!plateau.ColonneValide(c)) continue;
					plateau.Poser(c, pion);
					int score = Minimax(plateau, prof - 1, false, alpha, beta);
					plateau.Retirer(c);
					if (score > meilleur) meilleur = score;
					if (meilleur > alpha) alpha = meilleur;
					if (alpha >= beta) break;
				}
				return meilleur;
			}
			else
			{
				int meilleur = int.MaxValue;
				for (int c = 0; c < plateau.GetColonnes(); c++)
				{
					if (!plateau.ColonneValide(c)) continue;
					plateau.Poser(c, pionAdversaire);
					int score = Minimax(plateau, prof - 1, true, alpha, beta);
					plateau.Retirer(c);
					if (score < meilleur) meilleur = score;
					if (meilleur < beta) beta = meilleur;
					if (alpha >= beta) break;
				}
				return meilleur;
			}
		}

		// Heuristique differentielle  score IA - score adversaire
		// Valeur positive = avantage pour l'IA
		private int Heuristique(Plateau plateau)
		{
			return ScoreTotal(plateau, pion) - ScoreTotal(plateau, pionAdversaire);
		}

		// Calcule le score d'un joueur en analysant toutes les fenetres
		// de taille aligner dans les 4 directions
		private int ScoreTotal(Plateau plateau, Pion p)
		{
			int total = 0;
			Pion adv = (p == Pion.JOUEUR1) ? Pion.JOUEUR2 : Pion.JOUEUR1;
			int A = plateau.GetAligner();
			int[,] dirs = { { 0, 1 }, { 1, 0 }, { 1, 1 }, { -1, 1 } };

			for (int d = 0; d < 4; d++)
			{
				int dl = dirs[d, 0], dc = dirs[d, 1];
				for (int l = 0; l < plateau.GetLignes(); l++)
				{
					for (int c = 0; c < plateau.GetColonnes(); c++)
					{
						int el = l + (A - 1) * dl;
						int ec = c + (A - 1) * dc;
						if (el < 0 || el >= plateau.GetLignes()) continue;
						if (ec < 0 || ec >= plateau.GetColonnes()) continue;

						int cntP = 0, cntAdv = 0;
						for (int k = 0; k < A; k++)
						{
							Pion cellule = plateau.GetCase(l + k * dl, c + k * dc);
							if (cellule == p) cntP++;
							if (cellule == adv) cntAdv++;
						}
						// Fenetre utile seulement si aucun pion adverse dedans
						if (cntAdv == 0) total += ScoreFenetre(cntP, A);
					}
				}
			}

			// Bonus pour les pions en colonne centrale (position strategique)
			int colCentre = plateau.GetColonnes() / 2;
			for (int l = 0; l < plateau.GetLignes(); l++)
				if (plateau.GetCase(l, colCentre) == p) total += 3;

			return total;
		}

		// Valeur d'une fenetre selon le nombre de pions alignes
		private int ScoreFenetre(int cnt, int A)
		{
			if (cnt == A) return 1000;
			if (cnt == A - 1) return 50;
			if (cnt == A - 2) return 10;
			if (cnt == 1) return 1;
			return 0;
		}
	}

	// Classe Jeu  orchestre le menu, la configuration et la boucle de jeu
	class Jeu
	{
		private List<Joueur> joueurs;
		private int lignes;
		private int colonnes;
		private int aligner;

		public Jeu()
		{
			joueurs = new List<Joueur>();
			lignes = 6;
			colonnes = 7;
			aligner = 4;
		}

		public void Lancer()
		{
			AfficherBienvenue();
			bool rejouer = true;
			while (rejouer)
			{
				ConfigurerPartie();
				JouerPartie();
				rejouer = DemanderRejouer();
			}
			Console.WriteLine("Merci d'avoir joue ! A bientot.");
		}

		private void AfficherBienvenue()
		{
			Console.WriteLine("================================");
			Console.WriteLine("       PUISSANCE N - SAE 2.2   ");
			Console.WriteLine("================================\n");
		}

		private void ConfigurerPartie()
		{
			joueurs.Clear();

			Console.WriteLine("\nMode de jeu :");
			Console.WriteLine("  1 - Joueur vs Joueur");
			Console.WriteLine("  2 - Joueur vs IA");
			Console.WriteLine("  3 - IA vs IA");
			int choix = LireEntier("Votre choix : ", 1, 3, 2);

			if (choix == 1)
			{
				joueurs.Add(new JoueurHumain("Joueur 1", Pion.JOUEUR1));
				joueurs.Add(new JoueurHumain("Joueur 2", Pion.JOUEUR2));
			}
			else if (choix == 2)
			{
				Difficulte d = DemanderDifficulte("l'IA");
				joueurs.Add(new JoueurHumain("Joueur", Pion.JOUEUR1));
				joueurs.Add(new JoueurIA("IA", Pion.JOUEUR2, d));
			}
			else
			{
				Difficulte d1 = DemanderDifficulte("IA 1");
				Difficulte d2 = DemanderDifficulte("IA 2");
				joueurs.Add(new JoueurIA("IA 1", Pion.JOUEUR1, d1));
				joueurs.Add(new JoueurIA("IA 2", Pion.JOUEUR2, d2));
			}

			DemanderConfig();
			Console.WriteLine("\nC'est parti !");
		}

		// Boucle principale d'une partie
		private void JouerPartie()
		{
			Plateau plateau = new Plateau(lignes, colonnes, aligner);
			int tour = 0;

			while (true)
			{
				plateau.Afficher();
				Joueur joueurActif = joueurs[tour % 2];

				int col = joueurActif.ChoisirColonne(plateau);
				plateau.Poser(col, joueurActif.GetPion());

				if (plateau.AGagne(joueurActif.GetPion()))
				{
					plateau.Afficher();
					Console.WriteLine("*** " + joueurActif.GetNom() + " a gagne ! ***\n");
					return;
				}
				if (plateau.EstPleine())
				{
					plateau.Afficher();
					Console.WriteLine("*** Match nul ! ***\n");
					return;
				}
				tour++;
			}
		}

		private Difficulte DemanderDifficulte(string nomIA)
		{
			Console.WriteLine("\nNiveau de difficulte pour " + nomIA + " :");
			Console.WriteLine("  1 - Facile");
			Console.WriteLine("  2 - Moyen");
			Console.WriteLine("  3 - Difficile");
			int choix = LireEntier("Votre choix : ", 1, 3, 2);

			if (choix == 1) return Difficulte.FACILE;
			if (choix == 3) return Difficulte.DIFFICILE;
			return Difficulte.MOYEN;
		}

		private void DemanderConfig()
		{
			Console.WriteLine("\n--- Configuration de la grille ---");
			lignes = LireEntier("Nombre de lignes   [4-12, defaut 6] : ", 4, 12, 6);
			colonnes = LireEntier("Nombre de colonnes [4-15, defaut 7] : ", 4, 15, 7);

			int maxAlign = Math.Min(lignes, colonnes);
			aligner = LireEntier("Pions a aligner [3-" + maxAlign + ", defaut 4] : ",
								 3, maxAlign, 4);

			Console.WriteLine("\nGrille : " + lignes + " x " + colonnes
							  + "  |  Victoire : " + aligner + " pions alignes");
		}

		// Lit un entier dans [min, max], retourne defaut si saisie invalide
		private int LireEntier(string invite, int min, int max, int defaut)
		{
			Console.Write(invite);
			string saisie = Console.ReadLine();
			bool ok = int.TryParse(saisie, out int valeur);
			if (ok && valeur >= min && valeur <= max) return valeur;
			Console.WriteLine("Valeur invalide, valeur par defaut : " + defaut);
			return defaut;
		}

		private bool DemanderRejouer()
		{
			Console.Write("Rejouer ? (o/n) : ");
			string rep = Console.ReadLine();
			return (rep == "o" || rep == "O");
		}
	}

	class Program
	{
		static void Main(string[] args)
		{
			Jeu jeu = new Jeu();
			jeu.Lancer();
		}
	}
}