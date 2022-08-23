using OWML.Common;
using OWML.ModHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CutAchievements
{
	public class Core : ModBehaviour
	{
		public const string GoFast = "CUTACHIEVEMENTS.GO_FAST";
		public const string Strike = "CUTACHIEVEMENTS.STRIKE";
		public const string PlanetExplorer = "CUTACHIEVEMENTS.PLANET_EXPLORER";
		public const string Performance = "CUTACHIEVEMENTS.WHY";

		public static IAchievements API;
		public static IModHelper Helper;

		private static NomaiCairn[] _nomaiCairns;
		private static List<int> _knockedOverCairns = new List<int>();
		private static List<Sector.Name> _sectorsForExplorer = new List<Sector.Name> 
		{ 
			Sector.Name.HourglassTwin_A,
			Sector.Name.HourglassTwin_B,
			Sector.Name.TimberHearth,
			Sector.Name.TimberMoon,
			Sector.Name.BrittleHollow,
			Sector.Name.VolcanicMoon,
			Sector.Name.GiantsDeep,
			Sector.Name.QuantumMoon,
			Sector.Name.DarkBramble
		};

		private static List<Sector.Name> _visitedSectors = new List<Sector.Name>();

		public void Start()
		{
			Helper = ModHelper;
			SceneManager.sceneLoaded += OnSceneLoaded;
			API = ModHelper.Interaction.TryGetModApi<IAchievements>("xen.AchievementTracker");
			API.RegisterAchievement(GoFast, false, this);
			API.RegisterTranslation(GoFast, TextTranslation.Language.ENGLISH, "Zip Zoom", "Go really, really fast.");
			API.RegisterAchievement(Strike, false, this);
			API.RegisterTranslation(Strike, TextTranslation.Language.ENGLISH, "Strike!", "Knock over every Nomai cairn.");
			API.RegisterAchievement(PlanetExplorer, false, this);
			API.RegisterTranslation(PlanetExplorer, TextTranslation.Language.ENGLISH, "Space Exploredinaire", "Land on every planet and moon in one time loop.");
			API.RegisterAchievement(Performance, false, this);
			API.RegisterTranslation(Performance, TextTranslation.Language.ENGLISH, "Critical Performance Hit", "Have you, your ship, and the Little Scout all on seperate planets.");

			_knockedOverCairns = Helper.Storage.Load<List<int>>("cairns.json");

			if (_knockedOverCairns == null)
			{
				_knockedOverCairns = new List<int>();
				Helper.Storage.Save(_knockedOverCairns, "cairns.json");
			}

			Patches.Apply();
		}

		private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			if (scene.name == "SolarSystem")
			{
				_nomaiCairns = Resources.FindObjectsOfTypeAll<NomaiCairn>().Where(x => x.gameObject.activeSelf && x.transform.childCount != 0).ToArray();
			}

			API.UpdateProgress(PlanetExplorer, 0, 9, false);
		}

		public void FixedUpdate()
		{
			if (LoadManager.GetCurrentScene() != OWScene.SolarSystem)
			{
				return;
			}

			if (Locator.GetPlayerBody() == null)
			{
				return;
			}

			var playerSectors = Locator.GetPlayerSectorDetector()._sectorList;
			var playerMainSector = playerSectors.FirstOrDefault(x => x.GetName() != Sector.Name.Unnamed);

			CheckGoFast();
			CheckPerformance(playerMainSector);
			CheckExplorer(playerSectors);
		}

		private void CheckGoFast()
		{
			var playerBody = Locator.GetPlayerBody();
			if (playerBody.GetVelocity().sqrMagnitude >= 15000 * 15000)
			{
				API.EarnAchievement(GoFast);
			}
		}

		private void CheckPerformance(Sector playerMainSector)
		{
			var shipSectors = Locator.GetShipDetector().GetComponent<SectorDetector>()._sectorList;
			var probeSectors = Locator.GetProbe()._sectorDetector._sectorList;

			var shipMainSector = shipSectors.FirstOrDefault(x => x.GetName() != Sector.Name.Unnamed);
			var probeMainSector = probeSectors.FirstOrDefault(x => x.GetName() != Sector.Name.Unnamed);

			if (playerMainSector == default || shipMainSector == default || probeMainSector == default)
			{
				return;
			}

			if (playerMainSector.GetName() != shipMainSector.GetName()
				&& playerMainSector.GetName() != probeMainSector.GetName()
				&& probeMainSector.GetName() != shipMainSector.GetName())
			{
				API.EarnAchievement(Performance);
			}
		}

		private void CheckExplorer(List<Sector> playerSectors)
		{
			if (API.GetProgress(PlanetExplorer) == _sectorsForExplorer.Count)
			{
				return;
			}

			foreach (var sector in playerSectors)
			{
				if (!_sectorsForExplorer.Contains(sector.GetName()))
				{
					continue;
				}

				if (_visitedSectors.Contains(sector.GetName()))
				{
					continue;
				}

				_visitedSectors.Add(sector.GetName());

				Helper.Console.WriteLine($"Visited {sector.GetName()}");

				if (_visitedSectors.Count == _sectorsForExplorer.Count)
				{
					API.EarnAchievement(PlanetExplorer);
					API.UpdateProgress(PlanetExplorer, _visitedSectors.Count, _sectorsForExplorer.Count, false);
					continue;
				}

				API.UpdateProgress(PlanetExplorer, _visitedSectors.Count, _sectorsForExplorer.Count, false);
			}
		}

		public static void KnockOverCairn(NomaiCairn cairn)
		{
			if (_nomaiCairns == null)
			{
				Helper.Console.WriteLine($"Error - knocked over cairn when list is null?!", MessageType.Error);
				return;
			}

			var index = Array.IndexOf(_nomaiCairns, cairn);

			if (_knockedOverCairns.Contains(index))
			{
				return;
			}

			_knockedOverCairns.Add(index);
			Helper.Storage.Save(_knockedOverCairns, "cairns.json");
			
			var oldProgress = API.GetProgress(Strike);
			var newProgress = oldProgress + 1;

			if (newProgress == _nomaiCairns.Length)
			{
				API.UpdateProgress(Strike, newProgress, _nomaiCairns.Length, false);
				API.EarnAchievement(Strike);
				return;
			}

			API.UpdateProgress(Strike, newProgress, _nomaiCairns.Length, true);
		}
	}
}
