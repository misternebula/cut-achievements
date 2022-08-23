using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CutAchievements
{
	public static class Patches
	{
		public static void Apply()
		{
			Core.Helper.HarmonyHelper.AddPrefix<NomaiCairn>(nameof(NomaiCairn.KnockOver), typeof(Patches), nameof(Patches.KnockOver));
		}

		public static void KnockOver(NomaiCairn __instance)
		{
			Core.KnockOverCairn(__instance);
		}
	}
}
