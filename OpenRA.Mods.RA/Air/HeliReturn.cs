﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;
using OpenRA.Traits.Activities;
using OpenRA.Mods.RA.Activities;

namespace OpenRA.Mods.RA.Air
{
	public class HeliReturn : CancelableActivity
	{
		static Actor ChooseHelipad(Actor self)
		{
			var rearmBuildings = self.Info.Traits.Get<HelicopterInfo>().RearmBuildings;
			return self.World.Actors.Where( a => a.Owner == self.Owner ).FirstOrDefault(
				a => rearmBuildings.Contains(a.Info.Name) &&
					!Reservable.IsReserved(a));
		}

		public override IActivity Tick(Actor self)
		{
			if (IsCanceled) return NextActivity;
			var dest = ChooseHelipad(self);

			var initialFacing = self.Info.Traits.Get<AircraftInfo>().InitialFacing;

			if (dest == null)
				return Util.SequenceActivities(
					new Turn(initialFacing), 
					new HeliLand(true),
					NextActivity);

			var res = dest.TraitOrDefault<Reservable>();
			var heli = self.Trait<Helicopter>();
			if (res != null)
				heli.reservation = res.Reserve(dest, self, heli);

			var exit = dest.Info.Traits.WithInterface<ExitInfo>().FirstOrDefault();
			var offset = exit != null ? exit.SpawnOffset : int2.Zero;

			return Util.SequenceActivities(
				new HeliFly(dest.Trait<IHasLocation>().PxPosition + offset),
				new Turn(initialFacing),
				new HeliLand(false),
				new Rearm(),
				NextActivity);
		}
	}
}
