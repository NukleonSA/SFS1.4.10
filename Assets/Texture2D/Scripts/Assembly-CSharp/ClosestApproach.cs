using System;
using System.Collections.Generic;
using UnityEngine;

public static class ClosestApproach
{
	public static void DrawClosestApproachPlanet(List<Orbit> orbits, Planet targetPlanet)
	{
		DrawClosestApproachPlanetCalculate(orbits, targetPlanet);
	}

	private static void DrawClosestApproachPlanetCalculate(List<Orbit> orbits, Planet targetPlanet)
	{
		for (int i = 0; i < orbits.Count && !(orbits[i].nextPlanet == targetPlanet); i++)
		{
			if (!(orbits[i].planet != targetPlanet.parentBody))
			{
				CalculateClosestApproachPlanet(orbits[i], targetPlanet);
				break;
			}
		}
	}

	private static void CalculateClosestApproachPlanet(Orbit orbit, Planet targetPlanet)
	{
		List<double> crossTrueAnomalys = GetCrossTrueAnomalys(orbit, targetPlanet);
		if (crossTrueAnomalys.Count == 0)
		{
			return;
		}
		double num = double.PositiveInfinity;
		Double3 posA = Double3.zero;
		Double3 posB = Double3.zero;
		for (int i = 0; i < crossTrueAnomalys.Count; i++)
		{
			double nextTrueAnomalyPassageTime = orbit.GetNextTrueAnomalyPassageTime(Ref.controller.globalTime, crossTrueAnomalys[i]);
			if (!(nextTrueAnomalyPassageTime < Ref.controller.globalTime))
			{
				Double3 position = Kepler.GetPosition(Kepler.GetRadius(orbit.semiLatusRectum, orbit.eccentricity, crossTrueAnomalys[i]), crossTrueAnomalys[i], orbit.argumentOfPeriapsis);
				Double3 posOut = targetPlanet.GetPosOut(nextTrueAnomalyPassageTime);
				double sqrMagnitude2d = (position - posOut).sqrMagnitude2d;
				if (!(sqrMagnitude2d > num))
				{
					num = sqrMagnitude2d;
					posA = position;
					posB = posOut;
				}
			}
		}
		OrbitLines.DrawCounter(posA, posB, Ref.map.orbitLineMaterials[targetPlanet.orbitalDepth].color, orbit.planet, targetPlanet.data.BASE_DATA.name + "靠近\n", 1, 0.0);
	}

	private static List<double> GetCrossTrueAnomalys(Orbit orbit, Planet targetPlanet)
	{
		if (Utility.IsInsideRange(targetPlanet.data.ORBIT_DATA.orbitHeight, orbit.periapsis, orbit.apoapsis))
		{
			double trueAnomalyAtRadius = Kepler.GetTrueAnomalyAtRadius(targetPlanet.data.ORBIT_DATA.orbitHeight, orbit.semiLatusRectum, orbit.eccentricity);
			List<double> list = new List<double>();
			list.Add(0.0 - trueAnomalyAtRadius);
			list.Add(trueAnomalyAtRadius);
			return list;
		}
		if (orbit.CanPasSOI(targetPlanet.data.ORBIT_DATA.orbitHeight, targetPlanet.data.ORBIT_DATA.orbitHeight * 0.65))
		{
			List<double> list = new List<double>();
			list.Add((!(orbit.apoapsis > targetPlanet.data.ORBIT_DATA.orbitHeight)) ? ((float)Math.PI) : 0f);
			return list;
		}
		return new List<double>();
	}

	public static void DrawClosestApproachVessel(List<Orbit> orbits, Orbit targetOrbit)
	{
		DrawClosestApproachVesselCalculate(orbits, targetOrbit);
	}

	private static void DrawClosestApproachVesselCalculate(List<Orbit> orbits, Orbit targetOrbit)
	{
		if (targetOrbit == null)
		{
			return;
		}
		foreach (Orbit orbit in orbits)
		{
			if (orbit.planet == targetOrbit.planet)
			{
				CalculateClosestApproachVessel(orbit, targetOrbit, Ref.controller.globalTime);
				break;
			}
		}
	}

	private static void CalculateClosestApproachVessel(Orbit orbitA, Orbit orbitB, double time)
	{
		if (orbitA.orbitType != Orbit.Type.Eternal || orbitB.orbitType != Orbit.Type.Eternal)
		{
			return;
		}
		double num = Math.Max(Math.Min(orbitA.period, orbitB.period) * 2.5, Math.Max(orbitA.period, orbitB.period));
		double num2 = SampleWindow(orbitA, orbitB, time, num, 100);
		double num3 = double.PositiveInfinity;
		double num4 = num / 100.0;
		for (int i = 0; i < 15; i++)
		{
			num4 *= 0.5;
			double sqrMagnitude2d = GetRelativePositonInTime(orbitA, orbitB, num2 + num4).sqrMagnitude2d;
			if (sqrMagnitude2d < num3)
			{
				num3 = sqrMagnitude2d;
				num2 += num4;
			}
			double sqrMagnitude2d2 = GetRelativePositonInTime(orbitA, orbitB, num2 - num4).sqrMagnitude2d;
			if (sqrMagnitude2d2 < num3)
			{
				num3 = sqrMagnitude2d2;
				num2 -= num4;
			}
		}
		Debug.Log(num4);
		Debug.Log(num2);
		OrbitLines.DrawCounter(orbitA.GetPosOut((int)num2), orbitB.GetPosOut((int)num2), Color.white, orbitA.planet, "Closest靠近\n", 1, 0.0);
	}

	private static double SampleWindow(Orbit orbitA, Orbit orbitB, double windowStart, double windowSize, int sampleCount)
	{
		double num = double.PositiveInfinity;
		double result = windowStart;
		double num2 = windowSize / (double)sampleCount;
		for (int i = 0; i < sampleCount; i++)
		{
			double num3 = windowStart + num2 * (double)i;
			double sqrMagnitude2d = GetRelativePositonInTime(orbitA, orbitB, num3).sqrMagnitude2d;
			if (sqrMagnitude2d < num)
			{
				num = sqrMagnitude2d;
				result = num3;
			}
		}
		return result;
	}

	private static Double3 GetRelativePositonInTime(Orbit orbitA, Orbit orbitB, double time)
	{
		return orbitA.GetPosOut(time) - orbitB.GetPosOut(time);
	}
}
