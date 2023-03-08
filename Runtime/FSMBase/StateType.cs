using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
namespace FSM
{
	public abstract class StateType<T> : IComparable
	{
		public string Name { get; private set; }

		private static int _index;
		public int Index { get; private set; }
	
		protected StateType(string name)
		{
			Name = name;
			Index = _index++;
		}
	
		public override string ToString() => Name;
	
		public static int GetCount<T2>() where T2 : StateType<T> => 
			typeof(T2).GetFields(BindingFlags.Public |
			                     BindingFlags.Static |
			                     BindingFlags.DeclaredOnly)
				.Select(f => f.GetValue(null))
				.Cast<T2>().Count();
	
		public static IEnumerable<T2> GetAll<T2>() where T2 : StateType<T> =>
			typeof(T2).GetFields(BindingFlags.Public |
			                     BindingFlags.Static |
			                     BindingFlags.DeclaredOnly)
				.Select(f => f.GetValue(null))
				.Cast<T2>();

		public override bool Equals(object obj)
		{
			if (!(obj is StateType<T> otherValue))
			{
				return false;
			}

			var typeMatches = GetType().Equals(obj.GetType());
			var valueMatches = Index.Equals(otherValue.Index);

			return typeMatches && valueMatches;
		}

		public int CompareTo(object other) => Index.CompareTo(((StateType<T>)other).Index);

		// Other utility methods ...
	}
}
