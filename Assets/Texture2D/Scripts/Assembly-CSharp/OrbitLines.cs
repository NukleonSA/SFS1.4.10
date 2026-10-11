using System;
using System.Collections.Generic;
using UnityEngine;

public static class OrbitLines
{
	[Serializable]
	public class OrbitLinesPack
	{
		private bool hidden;

		public LineRenderer elliptical;

		public LineRenderer[] procedural;

		public OrbitLinesPack(Transform vesselOrbitLinePrefab, Transform counterPrefab)
		{
			elliptical = CreateVesselOrbitLine("Elliptical", vesselOrbitLinePrefab);
			elliptical.SetPositions(Ref.map.ellipsePoints);
			elliptical.endColor = new Color(1f, 1f, 1f, 0.2f);
			elliptical.transform.localScale = Vector3.one;
			procedural = new LineRenderer[3];
			for (int i = 0; i < procedural.Length; i++)
			{
				procedural[i] = CreateVesselOrbitLine("Orbit line procedual" + (i + 1), vesselOrbitLinePrefab);
				procedural[i].endColor = new Color(1f, 1f, 1f, 0.2f);
			}
		}

		private static LineRenderer CreateVesselOrbitLine(string name, Transform vesselOrbitLinePrefab)
		{
			LineRenderer component = UnityEngine.Object.Instantiate(vesselOrbitLinePrefab, Vector3.zero, Quaternion.identity, Ref.map.transform).GetChild(0).GetComponent<LineRenderer>();
			component.widthMultiplier = (float)(Ref.map.mapPosition.z * 0.003);
			component.sortingOrder = 2;
			component.sortingLayerName = "Map";
			component.transform.parent.gameObject.SetActive(false);
			component.transform.parent.name = name;
			component.transform.localScale = Vector3.one / 10000f;
			component.endColor = new Color(1f, 1f, 1f, 0.2f);
			return component;
		}

		public void UpdateLinesWidth(float newWidth)
		{
			elliptical.widthMultiplier = newWidth;
			for (int i = 0; i < procedural.Length; i++)
			{
				procedural[i].widthMultiplier = newWidth;
			}
		}

		public void HideAll()
		{
			if (hidden)
			{
				return;
			}
			hidden = true;
			if (elliptical.transform.parent.gameObject.activeSelf)
			{
				elliptical.transform.parent.gameObject.SetActive(false);
			}
			for (int i = 0; i < procedural.Length; i++)
			{
				if (procedural[i].transform.parent.gameObject.activeSelf)
				{
					procedural[i].transform.parent.gameObject.SetActive(false);
				}
			}
		}

		public void SetActive(bool ellipticalActive, bool[] procedualActive)
		{
			hidden = false;
			if (elliptical.transform.parent.gameObject.activeSelf != ellipticalActive)
			{
				elliptical.transform.parent.gameObject.SetActive(ellipticalActive);
			}
			for (int i = 0; i < procedural.Length; i++)
			{
				if (procedural[i].transform.parent.gameObject.activeSelf != procedualActive[i])
				{
					procedural[i].transform.parent.gameObject.SetActive(procedualActive[i]);
				}
			}
		}
	}

	public static List<Mesh> meshesPool = new List<Mesh>();

	public static int index = 0;

	public static void DrawOrbitLines(OrbitLinesPack orbitLines, List<Orbit> orbits)
	{
		if (orbits.Count == 0)
		{
			orbitLines.HideAll();
		}
		else
		{
			DrawKeplerOrbits(orbitLines, orbits);
		}
	}

	public static void DrawKeplerOrbits(OrbitLinesPack pack, List<Orbit> orbits)
	{
		bool ellipticalActive = false;
		bool[] array = new bool[3];
		for (int i = 0; i < orbits.Count && i < 3; i++)
		{
			if (orbits[i].eccentricity <= 1.0 && orbits[i].eccentricity > 0.9999999)
			{
				continue;
			}
			switch (orbits[i].orbitType)
			{
			case Orbit.Type.Eternal:
			{
				DrawOrbitStats(true, true, orbits[i]);
				pack.elliptical.material = Ref.map.orbitLineMaterials[orbits[i].planet.orbitalDepth];
				pack.elliptical.startColor = new Color(1f, 1f, 1f, 0.2f);
				pack.elliptical.transform.parent.localEulerAngles = new Vector3(0f, 0f, (float)orbits[i].argumentOfPeriapsis * 57.29578f);
				double num2 = Math.Sqrt(1.0 - orbits[i].eccentricity * orbits[i].eccentricity);
				double num3 = ((!(num2 > 0.0)) ? 0.0 : (orbits[i].semiMajorAxis * num2));
				pack.elliptical.transform.parent.localScale = new Vector3((float)(orbits[i].semiMajorAxis / 10000.0), (float)(num3 / 10000.0 * (double)(-Math.Sign(orbits[i].meanMotion))), 1f);
				if (i > 0 && orbits[i - 1].orbitType == Orbit.Type.Escape)
				{
					pack.elliptical.startColor = new Color(1f, 1f, 1f, 0.05f);
					pack.elliptical.transform.localEulerAngles = new Vector3(0f, 0f, (float)orbits[i].GetEccentricAnomalyOut(orbits[i - 1].orbitEndTime) * 57.29578f * (float)(-Math.Sign(orbits[i].meanMotion)));
				}
				Vector3 localPosition = new Vector3(Mathf.Cos((float)orbits[i].argumentOfPeriapsis), Mathf.Sin((float)orbits[i].argumentOfPeriapsis), 0f) * (float)((orbits[i].periapsis - orbits[i].apoapsis) / 20000.0);
				SetParentAndPosition(pack.elliptical.transform.parent, orbits[i].planet.mapHolder, localPosition);
				ellipticalActive = true;
				break;
			}
			case Orbit.Type.Escape:
				DrawOrbitStats(true, false, orbits[i]);
				pack.procedural[i].SetPositions(orbits[i].GenerateOrbitLinePoints(0.0 - orbits[i].endTrueAnomaly, orbits[i].endTrueAnomaly, 200));
				pack.procedural[i].startColor = new Color(1f, 1f, 1f, 0.05f);
				SetParentAndPosition(pack.procedural[i].transform.parent, orbits[i].planet.mapHolder, Vector3.zero);
				pack.procedural[i].material = Ref.map.orbitLineMaterials[orbits[i].planet.orbitalDepth];
				array[i] = true;
				break;
			case Orbit.Type.Encounter:
			{
				double num = Math.Max(Ref.controller.globalTime, orbits[i].orbitStartTime);
				double nextTrueAnomalyPassageTime = orbits[i].GetNextTrueAnomalyPassageTime(num, 0.0);
				double nextTrueAnomalyPassageTime2 = orbits[i].GetNextTrueAnomalyPassageTime(num, Math.PI);
				DrawOrbitStats(nextTrueAnomalyPassageTime > num && nextTrueAnomalyPassageTime < orbits[i].orbitEndTime, nextTrueAnomalyPassageTime2 > num && nextTrueAnomalyPassageTime2 < orbits[i].orbitEndTime, orbits[i]);
				pack.procedural[i].SetPositions(orbits[i].GenerateOrbitLinePoints(orbits[i].GetTrueAnomalyOut(num), orbits[i].endTrueAnomaly + (double)((float)Math.PI * 4f * (float)Math.Sign(orbits[i].meanMotion)), 200));
				pack.procedural[i].startColor = new Color(1f, 1f, 1f, (i <= 0) ? 0.05f : 0.2f);
				SetParentAndPosition(pack.procedural[i].transform.parent, orbits[i].planet.mapHolder, Vector3.zero);
				pack.procedural[i].material = Ref.map.orbitLineMaterials[orbits[i].planet.orbitalDepth];
				array[i] = true;
				Ref.map.elementManager.DrawTextElement("Encounter", 42, 1f, 0, pack.procedural[i].transform.TransformPoint(pack.procedural[i].GetPosition(200)));
				break;
			}
			}
		}
		pack.SetActive(ellipticalActive, array);
	}

	private static void DrawOrbitStats(bool showPeri, bool showApo, Orbit orbit)
	{
		if (showPeri && orbit.periapsis > orbit.planet.data.BASE_DATA.radius)
		{
			DrawCounter(Kepler.GetPosition(orbit.planet.data.BASE_DATA.radius, 0.0, orbit.argumentOfPeriapsis), Kepler.GetPosition(orbit.periapsis, 0.0, orbit.argumentOfPeriapsis), Color.white, orbit.planet, "近拱点\n", 0, orbit.periapsis * 0.15);
		}
		if (showApo && orbit.apoapsis > orbit.planet.data.BASE_DATA.radius)
		{
			DrawCounter(Kepler.GetPosition(orbit.planet.data.BASE_DATA.radius, 3.1415927410125732, orbit.argumentOfPeriapsis), Kepler.GetPosition(orbit.apoapsis, 3.1415927410125732, orbit.argumentOfPeriapsis), Color.white, orbit.planet, "Apoapsis\n", 0, orbit.apoapsis * 0.15);
		}
	}

	public static void DrawCounter(Double3 posA, Double3 posB, Color color, Planet planet, string name, int textPriority, double extraDistance)
	{
		double magnitude2d = (posA - posB).magnitude2d;
		float alpha = (float)((magnitude2d + extraDistance) / Ref.map.mapPosition.z / 0.08 - 1.0) / 0.25f;
		Ref.map.elementManager.DrawTextElement(name + Ref.controller.RoundUI(magnitude2d), 42, alpha, textPriority, planet.mapHolder.position + ((posA + posB) / 10000.0).toVector3 * 0.5f);
		DrawDottedLine((posA / 10000.0).toVector3, (posB / 10000.0).toVector3, planet.mapHolder.position, color);
	}

	public static void DrawDottedLine(Vector3 posA, Vector3 posB, Vector3 startPos, Color color)
	{
		float magnitude = (posA - posB).magnitude;
		float num = (float)(Ref.map.mapPosition.z * 0.002400000113993883 / 10000.0);
		Mesh mesh = meshesPool[index % (meshesPool.Count - 1)];
		index++;
		mesh.vertices = new Vector3[4]
		{
			new Vector3(0f - num, 0f, 0f),
			new Vector3(num, 0f, 0f),
			new Vector3(0f - num, magnitude, 0f),
			new Vector3(num, magnitude, 0f)
		};
		mesh.triangles = new int[6] { 0, 1, 2, 1, 3, 2 };
		mesh.uv = new Vector2[4]
		{
			Vector2.zero,
			Vector2.zero,
			new Vector2(magnitude * 10000f / (float)Ref.map.mapPosition.z * 70f, 0f),
			new Vector2(magnitude * 10000f / (float)Ref.map.mapPosition.z * 70f, 0f)
		};
		mesh.colors = new Color[4] { color, color, color, color };
		Graphics.DrawMesh(mesh, startPos + posA, Quaternion.Euler(0f, 0f, Mathf.Atan2(posA.y - posB.y, posA.x - posB.x) * 57.29578f + 90f), Ref.map.dottedLineMaterial, 5);
	}

	private static void SetCounter(Orbit orbit, double value, double trueAnomaly, Transform counter)
	{
		SetParentAndPosition(counter.transform, orbit.planet.mapHolder, Kepler.GetPosition(value / 10000.0, trueAnomaly, orbit.argumentOfPeriapsis).toVector3);
	}

	public static void SetParentAndPosition(Transform a, Transform parent, Vector3 localPosition)
	{
		if (a.parent != parent)
		{
			a.parent = parent;
		}
		a.localPosition = localPosition;
	}
}
