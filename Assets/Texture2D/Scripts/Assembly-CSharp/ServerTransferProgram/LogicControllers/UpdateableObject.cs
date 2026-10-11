using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using UnityEngine;

namespace ServerTransferProgram.LogicControllers
{
	public class UpdateableObject
	{
		private static List<UpdateableObject> toUpdate = new List<UpdateableObject>();

		public static bool Running = false;

		public static long LastCylceDuration = 0L;

		public static long CoreTicksPerSecond = 0L;

		private static long Ticks = 0L;

		private static Stopwatch tpsMeasurer = new Stopwatch();

		private static Stopwatch timer = new Stopwatch();

		private long ticks;

		private Stopwatch watchDog = new Stopwatch();

		public long TicksPerSecond;

		protected bool Active;

		protected long ID;

		private static long LastID = 0L;

		private MethodInfo aUpdate;

		private MethodInfo aDelete;

		private int hold;

		private Stopwatch loopHolder = new Stopwatch();

		public UpdateableObject()
		{
			toUpdate.Add(this);
			watchDog.Start();
			loopHolder.Start();
			ID = LastID;
			LastID++;
			MethodInfo method = GetType().GetMethod("Update", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (method != null)
			{
				aUpdate = method;
				Delayer[] array = (Delayer[])method.GetCustomAttributes(typeof(Delayer), true);
				if (array.Length != 0)
				{
					hold = array[0].holdMS;
				}
			}
			MethodInfo method2 = GetType().GetMethod("OnDelete", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (method2 != null)
			{
				aDelete = method2;
			}
			object[] customAttributes = GetType().GetCustomAttributes(typeof(DelayedStart), true);
			if (customAttributes.Length == 0)
			{
				SetActive(true);
			}
		}

		public static void StartUpdater()
		{
			ThreadPool.QueueUserWorkItem(StartUpdaterAsync);
		}

		private static void StartUpdaterAsync(object state)
		{
			Initiate();
			while (Running)
			{
				InternalTick();
			}
			UnityEngine.Debug.Log("Loop has ended");
		}

		public static void Initiate()
		{
			Running = true;
			tpsMeasurer.Reset();
			tpsMeasurer.Start();
		}

		public static void InternalTick()
		{
			try
			{
				timer.Reset();
				timer.Start();
				UpdateableObject[] array = toUpdate.ToArray();
				UpdateableObject[] array2 = array;
				foreach (UpdateableObject updateableObject in array2)
				{
					if (updateableObject == null)
					{
						UnityEngine.Debug.Log("How the fuck?");
					}
					try
					{
						updateableObject.InteralUpdate();
					}
					catch (Exception ex)
					{
						UnityEngine.Debug.Log("InternalUpdateException: " + ex.Message + ex.StackTrace + ex.InnerException);
					}
				}
				timer.Stop();
				LastCylceDuration = timer.ElapsedMilliseconds;
				Ticks++;
				if (tpsMeasurer.ElapsedMilliseconds >= 1000)
				{
					CoreTicksPerSecond = Ticks;
					Ticks = 0L;
					tpsMeasurer.Reset();
					tpsMeasurer.Start();
				}
			}
			catch (Exception ex2)
			{
				UnityEngine.Debug.Log(ex2.GetType().Name + "> " + ex2.Message + ex2.StackTrace + ex2.InnerException);
			}
		}

		private void InteralUpdate()
		{
			try
			{
				if (Active && aUpdate != null && loopHolder.ElapsedMilliseconds >= hold)
				{
					ticks++;
					loopHolder.Reset();
					loopHolder.Start();
					aUpdate.Invoke(this, null);
				}
				if (watchDog.ElapsedMilliseconds >= 1000)
				{
					TicksPerSecond = ticks;
					ticks = 0L;
					watchDog.Reset();
				}
			}
			catch (Exception ex)
			{
				UnityEngine.Debug.Log("WHOOPS, " + ex.Message + ex.StackTrace);
				UnityEngine.Debug.Log(ex.InnerException);
			}
		}

		public void SetActive(bool state)
		{
			Active = state;
		}

		public void Delete()
		{
			toUpdate.Remove(this);
			watchDog.Stop();
			if (aDelete != null)
			{
				aDelete.Invoke(this, null);
			}
		}
	}
}
