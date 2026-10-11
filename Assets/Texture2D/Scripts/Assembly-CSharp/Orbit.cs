using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Orbit
{
	[Serializable]
	public class Pass
	{
		public double startTime;

		public double endTime;

		public Planet passPlanet;

		public Pass(double startTime, double endTime, Planet passPlanet)
		{
			this.startTime = startTime;
			this.endTime = endTime;
			this.passPlanet = passPlanet;
		}
	}

	public enum Type
	{
		Eternal = 0,
		Encounter = 1,
		Escape = 2
	}

	[Header(" Static properties:")]
	public Planet planet;

	[Header(" Static properties:")]
	public Planet nextPlanet;

	public Type orbitType;

	public double orbitStartTime;

	public double orbitEndTime;

	public double endTrueAnomaly;

	public double stopTimeWarpTime;

	public double calculatePassesTime;

	[HideInInspector]
	public double semiMajorAxis;

	[HideInInspector]
	public double semiLatusRectum;

	[HideInInspector]
	public double argumentOfPeriapsis;

	public double meanMotion;

	public double periapsis;

	public double apoapsis;

	public double eccentricity;

	public double period;

	public double periapsisPassageTime;

	[Header(" Dynamic properties:")]
	private double trueAnomalyOut;

	[Header(" Dynamic properties:")]
	private double eccentricAnomnalyOut;

	private Double3 posOut;

	private Double3 velOut;

	private double lastCalculationTime = -1.0;

	public Orbit(Double3 posIn, Double3 velIn, double timeIn, Planet planet, Planet lastPlanet)
	{
		this.planet = planet;
		orbitStartTime = timeIn;
		double mass = planet.mass;
		Double3 b = Double3.Cross2d(posIn, velIn);
		Double3 @double = Double3.Cross(velIn, b) / mass - posIn.normalized2d;
		eccentricity = @double.magnitude2d;
		argumentOfPeriapsis = Math.Atan2(@double.y, @double.x);
		semiMajorAxis = (0.0 - mass) / (2.0 * (Math.Pow(velIn.magnitude2d, 2.0) / 2.0 - mass / posIn.magnitude2d));
		periapsis = semiMajorAxis * (1.0 - eccentricity);
		apoapsis = ((!(eccentricity < 1.0)) ? double.PositiveInfinity : (semiMajorAxis * (1.0 + eccentricity)));
		semiLatusRectum = Kepler.GetSemiLatusRectum(periapsis, eccentricity);
		period = Kepler.GetPeriod(eccentricity, semiMajorAxis, mass);
		meanMotion = Kepler.GetMeanMotion(period, eccentricity, mass, semiMajorAxis) * (double)Math.Sign(b.z);
		double trueAnomalyAtRadius = Kepler.GetTrueAnomalyAtRadius(posIn.magnitude2d, semiLatusRectum, eccentricity);
		double num = Kepler.GetMeanAnomaly(eccentricity, trueAnomalyAtRadius, posIn, argumentOfPeriapsis) / meanMotion;
		if (apoapsis > planet.SOI || eccentricity >= 1.0)
		{
			period = 0.0;
		}
		periapsisPassageTime = timeIn + num - period * 10.0;
		GetOrbitType(timeIn, lastPlanet);
		stopTimeWarpTime = GetStopTimeWarpTime();
	}

	public Orbit(Double3 posIn, Double3 velIn, Planet planet)
	{
		this.planet = planet;
		double mass = planet.mass;
		Double3 b = Double3.Cross(posIn, velIn);
		Double3 @double = Double3.Cross(velIn, b) / mass - posIn.normalized2d;
		eccentricity = @double.magnitude2d;
		argumentOfPeriapsis = Math.Atan2(@double.y, @double.x);
		semiMajorAxis = (0.0 - mass) / (2.0 * (Math.Pow(velIn.magnitude2d, 2.0) / 2.0 - mass / posIn.magnitude2d));
		periapsis = semiMajorAxis * (1.0 - eccentricity);
		apoapsis = ((!(eccentricity < 1.0)) ? double.PositiveInfinity : (semiMajorAxis * (1.0 + eccentricity)));
		if (apoapsis > planet.SOI || eccentricity >= 1.0)
		{
			orbitType = Type.Escape;
		}
		else
		{
			orbitType = Type.Eternal;
		}
		meanMotion = b.z;
		if (double.IsNaN(meanMotion))
		{
			Debug.Log("mean motion is nan 1");
		}
	}

	public void GetOrbitType(double timeIn, Planet lastPlanet)
	{
		orbitType = Type.Eternal;
		orbitEndTime = double.PositiveInfinity;
		calculatePassesTime = double.PositiveInfinity;
		if (apoapsis > this.planet.SOI || eccentricity > 1.0)
		{
			orbitEndTime = ((!this.planet.data.hasOrbitData) ? double.PositiveInfinity : (periapsisPassageTime + GetPassAnomaly(this.planet.SOI)));
			if (!double.IsNaN(meanMotion))
			{
				endTrueAnomaly = Kepler.GetTrueAnomalyAtRadius((this.planet.SOI == double.PositiveInfinity) ? 10000000000000.0 : this.planet.SOI, semiLatusRectum, eccentricity) * (double)Math.Sign(meanMotion);
			}
			orbitType = Type.Escape;
			nextPlanet = this.planet.parentBody;
		}
		double cutEndTime = ((orbitType != Type.Escape) ? (timeIn + period * 0.995) : double.PositiveInfinity);
		List<Pass> list = new List<Pass>();
		Planet[] satellites = this.planet.satellites;
		foreach (Planet planet in satellites)
		{
			if (CanPasSOI(planet.data.ORBIT_DATA.orbitHeight, planet.SOI))
			{
				double cutStartTime = timeIn + ((planet != lastPlanet) ? 0.0 : ((orbitType != Type.Escape) ? (period * 0.05) : double.PositiveInfinity));
				list.AddRange(CreatePasses(orbitType == Type.Escape, cutStartTime, cutEndTime, planet));
				if (orbitType == Type.Eternal)
				{
					calculatePassesTime = cutEndTime;
				}
			}
		}
		ProcessPasses(SortPasses(list));
	}

	public bool UpdatePass()
	{
		List<Pass> list = new List<Pass>();
		Planet[] satellites = this.planet.satellites;
		foreach (Planet planet in satellites)
		{
			if (CanPasSOI(planet.data.ORBIT_DATA.orbitHeight, planet.SOI))
			{
				list.AddRange(CreatePasses(false, calculatePassesTime, Ref.controller.globalTime + period * 0.995, planet));
			}
		}
		calculatePassesTime = Ref.controller.globalTime + period * 0.995;
		return ProcessPasses(SortPasses(list));
	}

	private List<Pass> CreatePasses(bool isEscape, double cutStartTime, double cutEndTime, Planet satellite)
	{
		double num = satellite.data.ORBIT_DATA.orbitHeight + satellite.SOI;
		double num2 = satellite.data.ORBIT_DATA.orbitHeight - satellite.SOI;
		double num3 = ((!isEscape) ? (periapsisPassageTime - period + (double)(int)((cutStartTime - (periapsisPassageTime - period)) / period) * period) : periapsisPassageTime);
		if (periapsis < num2 && apoapsis > num)
		{
			double passAnomaly = GetPassAnomaly(num);
			double passAnomaly2 = GetPassAnomaly(num2);
			List<Pass> list = CreatePass(num3 - passAnomaly + period, num3 - passAnomaly2 + period, cutStartTime, cutEndTime, isEscape, satellite);
			list.AddRange(CreatePass(num3 + passAnomaly2, num3 + passAnomaly, cutStartTime, cutEndTime, isEscape, satellite));
			return list;
		}
		if (periapsis > num2 && periapsis < num && apoapsis > num)
		{
			double passAnomaly3 = GetPassAnomaly(num);
			if (num3 + period * 0.5 > cutStartTime)
			{
				return CreatePass(num3 - passAnomaly3, num3 + passAnomaly3, cutStartTime, cutEndTime, isEscape, satellite);
			}
			return CreatePass(num3 - passAnomaly3 + period, num3 + passAnomaly3 + period, cutStartTime, cutEndTime, isEscape, satellite);
		}
		if (!isEscape)
		{
			if (apoapsis > num2 && apoapsis < num && periapsis < num2)
			{
				double passAnomaly4 = GetPassAnomaly(num2);
				return CreatePass(num3 + passAnomaly4, num3 + period - passAnomaly4, cutStartTime, cutEndTime, false, satellite);
			}
			if (apoapsis < num && periapsis > num2)
			{
				List<Pass> list2 = new List<Pass>();
				list2.Add(new Pass(cutStartTime, cutEndTime, satellite));
				return list2;
			}
		}
		return new List<Pass>();
	}

	private List<Pass> CreatePass(double passStartTime, double passEndTime, double cutStartTime, double cutEndTime, bool isEscape, Planet satellite)
	{
		List<Pass> list = new List<Pass>();
		if (cutStartTime < passEndTime && cutEndTime > passStartTime)
		{
			list.Add(new Pass((!(cutStartTime < passStartTime)) ? cutStartTime : passStartTime, (!(cutEndTime > passEndTime)) ? cutEndTime : passEndTime, satellite));
		}
		if (!isEscape && cutStartTime < passEndTime + period && cutEndTime > passStartTime + period)
		{
			list.Add(new Pass((!(cutStartTime < passStartTime + period)) ? cutStartTime : (passStartTime + period), (!(cutEndTime > passEndTime + period)) ? cutEndTime : (passEndTime + period), satellite));
		}
		return list;
	}

	private bool ProcessPasses(List<Pass> passes)
	{
		for (int i = 0; i < passes.Count; i++)
		{
			double num = ProcessPasss(passes[i]);
			if (num > 0.0)
			{
				orbitEndTime = num;
				endTrueAnomaly = GetTrueAnomalyOut(num);
				orbitType = Type.Encounter;
				nextPlanet = passes[i].passPlanet;
				calculatePassesTime = double.PositiveInfinity;
				return true;
			}
			if (num < 0.0 && calculatePassesTime != double.PositiveInfinity)
			{
				calculatePassesTime = 0.0 - num;
			}
		}
		return false;
	}

	private double ProcessPasss(Pass pass)
	{
		double num = pass.passPlanet.data.ORBIT_DATA.orbitHeight - pass.passPlanet.SOI;
		double num2 = planet.mass / (num * num) * 2.0;
		int num3 = 0;
		double num4 = pass.startTime;
		while (num4 < pass.endTime)
		{
			Double3 @double = pass.passPlanet.GetPosOut(num4) - GetPosOut(num4);
			double num5 = @double.magnitude2d - pass.passPlanet.SOI;
			Debug.DrawLine((GetPosOut(num4) / 10000.0).toVector3, (pass.passPlanet.GetPosOut(num4) / 10000.0).toVector3, (!(num5 < 5.0)) ? Color.green : Color.red);
			if (num5 < 5.0)
			{
				return num4;
			}
			Double3 double2 = pass.passPlanet.GetVelOut(num4) - GetVelOut(num4);
			double num6 = Math.Atan2(@double.x, @double.y) + Math.PI / 2.0;
			double num7 = double2.x * Math.Cos(num6) - double2.y * Math.Sin(num6);
			double num8 = num7 * num7 / (num2 * 2.0);
			double num9 = Math.Sqrt(2.0 * (num5 + num8) / num2);
			double num10 = num7 / num2;
			num4 += num9 - num10;
			num3++;
			if (num3 > 50)
			{
				Debug.Log("Could not calculate till the end");
				return 0.0 - num4;
			}
		}
		return 0.0;
	}

	private List<Pass> SortPasses(List<Pass> passes)
	{
		List<Pass> list = new List<Pass>();
		double num = 0.0;
		for (int i = 0; i < passes.Count; i++)
		{
			Pass pass = null;
			foreach (Pass pass2 in passes)
			{
				if (!(pass2.startTime > num))
				{
					continue;
				}
				if (pass != null)
				{
					if (pass2.startTime < pass.startTime)
					{
						pass = pass2;
					}
				}
				else
				{
					pass = pass2;
				}
			}
			num = pass.startTime;
			list.Add(pass);
		}
		return list;
	}

	public double GetPassAnomaly(double height)
	{
		double trueAnomalyAtRadius = Kepler.GetTrueAnomalyAtRadius(height, semiLatusRectum, eccentricity);
		double meanAnomaly = Kepler.GetMeanAnomaly(eccentricity, trueAnomalyAtRadius);
		return Math.Abs(meanAnomaly / meanMotion);
	}

	public bool CanPasSOI(double orbitHeight, double SOI)
	{
		if (periapsis < orbitHeight + SOI && apoapsis > orbitHeight - SOI)
		{
			return true;
		}
		return false;
	}

	public double GetStopTimeWarpTime()
	{
		double num = planet.data.BASE_DATA.radius + planet.data.BASE_DATA.timewarpHeight;
		if (periapsis + 10.0 < num)
		{
			double trueAnomalyAtRadius = Kepler.GetTrueAnomalyAtRadius(num, semiLatusRectum, eccentricity);
			double num2 = GetNextTrueAnomalyPassageTime(orbitStartTime, trueAnomalyAtRadius);
			double num3 = GetNextTrueAnomalyPassageTime(orbitStartTime, 0.0 - trueAnomalyAtRadius);
			if (num2 < orbitStartTime)
			{
				num2 += period;
			}
			if (num3 < orbitStartTime)
			{
				num3 += period;
			}
			if (num2 < orbitStartTime)
			{
				num2 = double.PositiveInfinity;
			}
			if (num3 < orbitStartTime)
			{
				num3 = double.PositiveInfinity;
			}
			return Math.Min(num2, num3);
		}
		return double.PositiveInfinity;
	}

	public static List<Orbit> CalculateOrbits(Double3 posIn, Double3 velIn, Planet initialPlanet)
	{
		List<Orbit> list = new List<Orbit>();
		if (double.IsNaN(posIn.x * velIn.y - posIn.y * velIn.x))
		{
			return list;
		}
		list.Add(new Orbit(posIn, velIn, Ref.controller.globalTime, initialPlanet, null));
		return CalculateOrbits(list);
	}

	public static List<Orbit> CalculateOrbits(List<Orbit> orbitsIn)
	{
		while (orbitsIn.Count < 3)
		{
			Orbit orbit = orbitsIn[orbitsIn.Count - 1];
			if (orbit.orbitType == Type.Escape && orbit.planet.data.hasOrbitData)
			{
				if (orbitsIn.Count >= 2 && orbitsIn[orbitsIn.Count - 2].orbitType == Type.Encounter)
				{
					return orbitsIn;
				}
				Double3 position = Kepler.GetPosition(orbit.planet.SOI, orbit.endTrueAnomaly, orbit.argumentOfPeriapsis);
				Double3 velIn = orbit.GetVelOut(orbit.orbitEndTime);
				position += orbit.planet.GetPosOut(orbit.orbitEndTime);
				velIn += orbit.planet.GetVelOut(orbit.orbitEndTime);
				if (double.IsNaN(position.x * velIn.y - position.y * velIn.x))
				{
					return orbitsIn;
				}
				orbitsIn.Add(new Orbit(position, velIn, orbit.orbitEndTime, orbit.nextPlanet, orbit.planet));
				continue;
			}
			if (orbit.orbitType == Type.Encounter)
			{
				if (orbitsIn.Count >= 2 && orbitsIn[orbitsIn.Count - 2].planet == orbit.nextPlanet)
				{
					return orbitsIn;
				}
				double eccentricAnomalyFromTrueAnomaly = Kepler.GetEccentricAnomalyFromTrueAnomaly(orbit.endTrueAnomaly, orbit.eccentricity);
				double radius = Kepler.GetRadius(orbit.semiLatusRectum, orbit.eccentricity, orbit.endTrueAnomaly);
				Double3 position2 = Kepler.GetPosition(radius, orbit.endTrueAnomaly, orbit.argumentOfPeriapsis);
				Double3 velocity = Kepler.GetVelocity(orbit.semiMajorAxis, radius, orbit.meanMotion, eccentricAnomalyFromTrueAnomaly, orbit.endTrueAnomaly, orbit.eccentricity, orbit.argumentOfPeriapsis);
				position2 -= orbit.nextPlanet.GetPosOut(orbit.orbitEndTime);
				velocity -= orbit.nextPlanet.GetVelOut(orbit.orbitEndTime);
				if (double.IsNaN(position2.x * velocity.y - position2.y * velocity.x))
				{
					return orbitsIn;
				}
				orbitsIn.Add(new Orbit(position2, velocity, orbit.orbitEndTime, orbit.nextPlanet, null));
				continue;
			}
			return orbitsIn;
		}
		return orbitsIn;
	}

	public Double3 GetVelOut(double newTime)
	{
		UpdateDynamicVariables(newTime);
		return velOut;
	}

	public Double3 GetPosOut(double newTime)
	{
		UpdateDynamicVariables(newTime);
		return posOut;
	}

	public double GetTrueAnomalyOut(double newTime)
	{
		UpdateDynamicVariables(newTime);
		return trueAnomalyOut;
	}

	public double GetEccentricAnomalyOut(double newTime)
	{
		UpdateDynamicVariables(newTime);
		return eccentricAnomnalyOut;
	}

	private void UpdateDynamicVariables(double newTime)
	{
		if (lastCalculationTime == newTime)
		{
			return;
		}
		lastCalculationTime = newTime;
		double eccentricAnomaly = Kepler.GetEccentricAnomaly((newTime - periapsisPassageTime) * meanMotion, eccentricity);
		if (!double.IsNaN(eccentricAnomaly))
		{
			eccentricAnomnalyOut = eccentricAnomaly;
			trueAnomalyOut = Kepler.GetTrueAnomaly(eccentricAnomnalyOut, eccentricity);
			double radius = Kepler.GetRadius(semiLatusRectum, eccentricity, trueAnomalyOut);
			posOut = Kepler.GetPosition(radius, trueAnomalyOut, argumentOfPeriapsis);
			if (eccentricity < 1.0)
			{
				velOut = Kepler.GetVelocity(semiMajorAxis, radius, meanMotion, eccentricAnomnalyOut, trueAnomalyOut, eccentricity, argumentOfPeriapsis);
				return;
			}
			double eccentricAnomaly2 = Kepler.GetEccentricAnomaly((newTime + 1.0 - periapsisPassageTime) * meanMotion, eccentricity);
			double trueAnomaly = Kepler.GetTrueAnomaly(eccentricAnomaly2, eccentricity);
			double radius2 = Kepler.GetRadius(semiLatusRectum, eccentricity, trueAnomaly);
			velOut = Kepler.GetSafeEscapeVleocity(radius, radius2, trueAnomalyOut, trueAnomaly, argumentOfPeriapsis);
		}
	}

	public double GetNextTrueAnomalyPassageTime(double referenceTime, double trueAnomaly)
	{
		return GetLastTrueAnomalyPassageTime(referenceTime, trueAnomaly) + period;
	}

	public double GetLastTrueAnomalyPassageTime(double referenceTime, double trueAnomaly)
	{
		double num = periapsisPassageTime + Kepler.GetMeanAnomaly(eccentricity, trueAnomaly) / meanMotion;
		if (orbitType != Type.Escape)
		{
			return num + (double)(int)((referenceTime - num) / period) * period;
		}
		return num;
	}

	public Vector3[] GenerateOrbitLinePoints(double fromTrueAnomaly, double toTrueAnomaly, int resolution)
	{
		Vector3[] array = new Vector3[resolution + 1];
		double num = (toTrueAnomaly - fromTrueAnomaly) % (Math.PI * 2.0) / (double)resolution;
		for (int i = 0; i < resolution + 1; i++)
		{
			double v = fromTrueAnomaly + num * (double)i;
			double radius = Kepler.GetRadius(semiLatusRectum, eccentricity, v);
			array[i] = Kepler.GetPosition(radius, v, argumentOfPeriapsis).toVector3;
		}
		return array;
	}
}
