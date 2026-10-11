using System;
using System.Collections.Generic;
using UnityEngine;

public static class TransferWindow
{
	[Serializable]
	public class Data
	{
		public TransferType transferType;

		public Planet departure;

		public Planet target;

		public Planet firstNeighbour;

		public Planet secondNeighbour;

		public double phaseAngle;

		private Data(TransferType transferType, Planet departure, Planet target, Planet firstNeighbour, Planet secondNeighbour)
		{
			this.transferType = transferType;
			this.departure = departure;
			this.target = target;
			this.firstNeighbour = firstNeighbour;
			this.secondNeighbour = secondNeighbour;
			phaseAngle = double.NaN;
			phaseAngle = ((transferType != TransferType.ToNeighbour) ? double.NaN : Kepler.GetPhaseAngle(firstNeighbour, secondNeighbour));
		}

		private static Data Empty()
		{
			return new Data(TransferType.None, null, null, null, null);
		}

		public static Data GetTransferType(Planet departure, Planet target)
		{
			if (departure == null || target == null)
			{
				return Empty();
			}
			if (departure == target)
			{
				return Empty();
			}
			if (departure == target.parentBody)
			{
				return new Data(TransferType.ToSatellite, departure, target, null, null);
			}
			if (departure.parentBody == target)
			{
				return new Data(TransferType.ToParent, departure, target, null, null);
			}
			if (departure.parentBody == target.parentBody)
			{
				return new Data(TransferType.ToNeighbour, departure, target, departure, target);
			}
			if (departure.parentBody.parentBody == target.parentBody && departure.parentBody != target)
			{
				return new Data(TransferType.ToNeighbour, departure, target, departure.parentBody, target);
			}
			if (departure.parentBody == target.parentBody.parentBody && departure != target.parentBody)
			{
				return new Data(TransferType.ToNeighbour, departure, target, departure, target.parentBody);
			}
			if (departure.parentBody.parentBody == target.parentBody.parentBody)
			{
				return new Data(TransferType.ToNeighbour, departure, target, departure.parentBody, target.parentBody);
			}
			return Empty();
		}
	}

	public enum TransferType
	{
		None = 0,
		ToSatellite = 1,
		ToNeighbour = 2,
		ToParent = 3
	}

	public static void PositionPlanetMarker(ref bool show, ref MeshFilter markerPlanet, Data transferWindow)
	{
		if (transferWindow.transferType == TransferType.ToNeighbour)
		{
			double num = transferWindow.phaseAngle + Ref.controller.globalTime * transferWindow.secondNeighbour.meanMotion;
			markerPlanet.transform.localEulerAngles = new Vector3(0f, 0f, (float)num * 57.29578f);
			show = true;
		}
	}

	public static void PositionOrbitMarker(ref bool show, ref MeshFilter meshFilterOrbit, List<Orbit> orbits, Data transferWindow)
	{
		if (transferWindow.transferType != TransferType.None && orbits.Count != 0 && orbits[0].orbitType != Orbit.Type.Encounter)
		{
			double ejectionOrbitHeight = Math.Min(orbits[0].periapsis * 1.35, orbits[0].apoapsis);
			UpdateLocalTransferWindowMarker(ref show, ref meshFilterOrbit, ejectionOrbitHeight, orbits[0], transferWindow);
		}
	}

	public static void SetParents(Data transferWindowData, Transform markerPlanet, Transform markerOrbit, Material meshPlanetMaterial)
	{
		if (transferWindowData.transferType != TransferType.None)
		{
			markerOrbit.parent = transferWindowData.departure.mapHolder;
			markerOrbit.localPosition = Vector3.zero;
			if (transferWindowData.transferType == TransferType.ToNeighbour)
			{
				bool flag = transferWindowData.firstNeighbour.data.ORBIT_DATA.orbitHeight > transferWindowData.secondNeighbour.data.ORBIT_DATA.orbitHeight;
				float num = (float)(transferWindowData.firstNeighbour.data.ORBIT_DATA.orbitHeight / 10000.0);
				markerPlanet.localScale = new Vector3(num, (!flag) ? (0f - num) : num, 1f);
				markerPlanet.parent = transferWindowData.firstNeighbour.parentBody.mapHolder;
				markerPlanet.localPosition = Vector3.zero;
				UpdateTransferWindowAlpha(meshPlanetMaterial, transferWindowData);
			}
		}
	}

	public static void GenerateMeshPlanetTW(ref MeshFilter meshFilterPlanet, float widthPercent, float angularSize, float size, Transform parent)
	{
		Vector3[] array = new Vector3[42];
		Color[] array2 = new Color[42];
		for (int i = 0; i < 21; i++)
		{
			array[i * 2] = new Vector3(Mathf.Cos((float)i * angularSize), Mathf.Sin((float)i * angularSize), 0f);
			array[i * 2 + 1] = array[i * 2] * (1f - widthPercent);
			array2[i * 2] = Color.white;
			array2[i * 2 + 1] = new Color(1f, 1f, 1f, 0f);
		}
		List<int> list = new List<int>();
		for (int j = 0; j < 20; j++)
		{
			list.Add(j * 2);
			list.Add(j * 2 + 2);
			list.Add(j * 2 + 1);
			list.Add(j * 2 + 1);
			list.Add(j * 2 + 2);
			list.Add(j * 2 + 3);
		}
		if (meshFilterPlanet.mesh == null)
		{
			meshFilterPlanet.mesh = new Mesh();
		}
		meshFilterPlanet.mesh.vertices = array;
		meshFilterPlanet.mesh.triangles = list.ToArray();
		meshFilterPlanet.mesh.colors = array2;
		meshFilterPlanet.mesh.RecalculateNormals();
		meshFilterPlanet.mesh.RecalculateBounds();
		meshFilterPlanet.transform.localScale = Vector3.one * size;
		meshFilterPlanet.transform.parent = parent;
		meshFilterPlanet.transform.localPosition = Vector3.zero;
	}

	private static void GenerateMeshOrbitTW(ref MeshFilter meshFilterOrbit, ref bool show, float ejectionAngle, Orbit orbit)
	{
		for (int i = 0; i < 2; i++)
		{
			double num = Math.Abs(Kepler.GetRadius(Kepler.GetSemiLatusRectum(orbit.periapsis, orbit.eccentricity), orbit.eccentricity, (double)(ejectionAngle + (float)i * 0.2f * (float)Math.Sign(orbit.meanMotion)) - orbit.argumentOfPeriapsis));
			if (num > orbit.planet.SOI || num > orbit.periapsis * 1.35)
			{
				return;
			}
		}
		Vector3[] array = new Vector3[42];
		for (int j = 0; j < 21; j++)
		{
			float num2 = ejectionAngle + (float)j * 0.01f * (float)Math.Sign(orbit.meanMotion);
			array[j * 2] = new Vector3(Mathf.Cos(num2), Mathf.Sin(num2), 0f) * (float)(Kepler.GetRadius(Kepler.GetSemiLatusRectum(orbit.periapsis, orbit.eccentricity), orbit.eccentricity, (double)num2 - orbit.argumentOfPeriapsis) / 10000.0);
			array[j * 2 + 1] = array[j * 2] * 0.915f;
		}
		meshFilterOrbit.mesh.vertices = array;
		meshFilterOrbit.mesh.RecalculateNormals();
		meshFilterOrbit.mesh.RecalculateBounds();
		show = true;
	}

	private static void UpdateLocalTransferWindowMarker(ref bool show, ref MeshFilter meshFilterOrbit, double ejectionOrbitHeight, Orbit orbit, Data transferWindow)
	{
		if (transferWindow.transferType != TransferType.ToNeighbour || !(transferWindow.departure != transferWindow.firstNeighbour))
		{
			if (transferWindow.transferType == TransferType.ToNeighbour || transferWindow.transferType == TransferType.ToParent)
			{
				Planet planet = ((transferWindow.transferType != TransferType.ToNeighbour) ? transferWindow.departure : transferWindow.firstNeighbour);
				double orbitHeight = planet.data.ORBIT_DATA.orbitHeight;
				double num = ((transferWindow.transferType != TransferType.ToNeighbour) ? (transferWindow.target.data.BASE_DATA.radius + transferWindow.target.data.ATMOSPHERE_DATA.PHYSICS.height) : transferWindow.secondNeighbour.data.ORBIT_DATA.orbitHeight);
				double num2 = Math.Min(num, orbitHeight);
				double num3 = (orbitHeight + num) * 0.5;
				double e = 1.0 - num2 / num3;
				double mass = planet.parentBody.mass;
				double meanMotion = Kepler.GetMeanMotion(Kepler.GetPeriod(e, num3, mass), e, mass, num3);
				bool flag = num > orbitHeight;
				double magnitude2d = Kepler.GetVelocity(num3, orbitHeight, meanMotion, (!flag) ? Math.PI : 0.0, (!flag) ? Math.PI : 0.0, e, 0.0).magnitude2d;
				double escapeVelocity = magnitude2d - (0.0 - planet.orbitalVelocity);
				float num4 = (float)(Kepler.GetEjectionAngle(escapeVelocity, ejectionOrbitHeight, planet.mass, planet.SOI) * (double)Math.Sign(0.0 - orbit.meanMotion) + ((!flag) ? Math.PI : 0.0));
				float ejectionAngle = num4 + (float)(Ref.controller.globalTime * planet.meanMotion) - (float)Math.PI / 2f;
				GenerateMeshOrbitTW(ref meshFilterOrbit, ref show, ejectionAngle, orbit);
			}
			else if (transferWindow.transferType == TransferType.ToSatellite && !(ejectionOrbitHeight > transferWindow.target.data.ORBIT_DATA.orbitHeight * 0.75))
			{
				float num5 = (float)Kepler.GetPhaseAngle(ejectionOrbitHeight, transferWindow.target.data.ORBIT_DATA.orbitHeight);
				float ejectionAngle2 = num5 + (float)(Ref.controller.globalTime * transferWindow.target.meanMotion);
				GenerateMeshOrbitTW(ref meshFilterOrbit, ref show, ejectionAngle2, orbit);
			}
		}
	}

	public static void UpdateTransferWindowAlpha(Material material, Data data)
	{
		material.color = new Color(1f, 1f, 1f, 0.05f + Mathf.Clamp01((float)(Ref.map.mapPosition.z / (data.firstNeighbour.data.ORBIT_DATA.orbitHeight * 0.5))) * 0.2f);
	}
}
