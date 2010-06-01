﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.TaskScheduler
{
	/// <summary>
	/// Provides the methods that are used to add to, remove from, and get the triggers of a task.
	/// </summary>
	public sealed class TriggerCollection : IEnumerable<Trigger>, IDisposable
	{
		private V1Interop.ITask v1Task = null;
		private V2Interop.ITaskDefinition v2Def = null;
		private V2Interop.ITriggerCollection v2Coll = null;

		internal TriggerCollection(V1Interop.ITask iTask)
		{
			v1Task = iTask;
		}

		internal TriggerCollection(V2Interop.ITaskDefinition iTaskDef)
		{
			v2Def = iTaskDef;
			v2Coll = v2Def.Triggers;
		}

		/// <summary>
		/// Releases all resources used by this class.
		/// </summary>
		public void Dispose()
		{
			if (v2Coll != null) Marshal.ReleaseComObject(v2Coll);
			v2Def = null;
			v1Task = null;
		}

		/// <summary>
		/// Gets the collection enumerator for this collection.
		/// </summary>
		/// <returns>The <see cref="IEnumerator{T}"/> for this collection.</returns>
		public IEnumerator<Trigger> GetEnumerator()
		{
			if (v1Task != null)
				return new V1TriggerEnumerator(v1Task);
			return new V2TriggerEnumerator(v2Coll);
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		internal sealed class V1TriggerEnumerator : IEnumerator<Trigger>
		{
			private V1Interop.ITask iTask;
			private short curItem = -1;

			internal V1TriggerEnumerator(V1Interop.ITask task)
			{
				iTask = task;
			}

			public Trigger Current
			{
				get
				{
					return Trigger.CreateTrigger(iTask.GetTrigger((ushort)curItem));
				}
			}

			/// <summary>
			/// Releases all resources used by this class.
			/// </summary>
			public void Dispose()
			{
				iTask = null;
			}

			object System.Collections.IEnumerator.Current
			{
				get { return this.Current; }
			}

			public bool MoveNext()
			{
				if (++curItem >= iTask.GetTriggerCount())
					return false;
				return true;
			}

			public void Reset()
			{
				curItem = -1;
			}
		}

		internal sealed class V2TriggerEnumerator : IEnumerator<Trigger>
		{
			private System.Collections.IEnumerator iEnum;

			internal V2TriggerEnumerator(V2Interop.ITriggerCollection iColl)
			{
				iEnum = iColl.GetEnumerator();
			}

			#region IEnumerator<Trigger> Members

			public Trigger Current
			{
				get
				{
					return Trigger.CreateTrigger((V2Interop.ITrigger)iEnum.Current);
				}
			}

			#endregion

			#region IDisposable Members

			/// <summary>
			/// Releases all resources used by this class.
			/// </summary>
			public void Dispose()
			{
				iEnum = null;
			}

			#endregion

			#region IEnumerator Members

			object System.Collections.IEnumerator.Current
			{
				get { return this.Current; }
			}

			public bool MoveNext()
			{
				return iEnum.MoveNext();
			}

			public void Reset()
			{
				iEnum.Reset();
			}

			#endregion
		}

		/// <summary>
		/// Gets the number of triggers in the collection.
		/// </summary>
		public int Count
		{
			get
			{
				if (v2Coll != null)
					return v2Coll.Count;
				return (int)v1Task.GetTriggerCount();
			}
		}

		/// <summary>
		/// Add an unbound <see cref="Trigger"/> to the task.
		/// </summary>
		/// <param name="unboundTrigger"><see cref="Trigger"/> derivative to add to the task.</param>
		/// <returns>Bound trigger.</returns>
		public Trigger Add(Trigger unboundTrigger)
		{
			if (v2Def != null)
				unboundTrigger.Bind(v2Def);
			else
				unboundTrigger.Bind(v1Task);
			return unboundTrigger;
		}

		/// <summary>
		/// Add a new trigger to the collections of triggers for the task.
		/// </summary>
		/// <param name="taskTriggerType">The type of trigger to create.</param>
		/// <returns>A <see cref="Trigger"/> instance of the specified type.</returns>
		public Trigger AddNew(TaskTriggerType taskTriggerType)
		{
			if (v1Task != null)
			{
				ushort idx;
				return Trigger.CreateTrigger(v1Task.CreateTrigger(out idx), Trigger.ConvertToV1TriggerType(taskTriggerType));
			}

			return Trigger.CreateTrigger(v2Coll.Create(taskTriggerType));
		}

		internal void Bind()
		{
			foreach (Trigger t in this)
				t.SetV1TriggerData();
		}

		/// <summary>
		/// Clears all triggers from the task.
		/// </summary>
		public void Clear()
		{
			if (v2Coll != null)
				v2Coll.Clear();
			else
			{
				for (int i = this.Count - 1; i >= 0; i--)
					RemoveAt(i);
			}
		}

		/// <summary>
		/// Gets a specified trigger from the collection.
		/// </summary>
		/// <param name="index">The index of the trigger to be retrieved.</param>
		/// <returns>Specialized <see cref="Trigger"/> instance.</returns>
		public Trigger this[int index]
		{
			get { if (v2Coll != null) return Trigger.CreateTrigger(v2Coll[++index]); return Trigger.CreateTrigger(v1Task.GetTrigger((ushort)index)); }
		}

		/// <summary>
		/// Removes the trigger at a specified index.
		/// </summary>
		/// <param name="index">Index of trigger to remove.</param>
		/// <exception cref="ArgumentOutOfRangeException">Index out of range.</exception>
		public void RemoveAt(int index)
		{
			if (index >= this.Count)
				throw new ArgumentOutOfRangeException("index", index, "Failed to remove Trigger. Index out of range.");
			if (v2Coll != null)
				v2Coll.Remove(++index);
			else
				v1Task.DeleteTrigger((ushort)index); //Remove the trigger from the Task Scheduler
		}
	}
}
