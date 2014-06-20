﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blender
{
	public class BlendValue
	{
		public IBlendType Type;
		public object RawValue;

		public BlendValue(IBlendType type, object value)
		{
			Type = type;
			RawValue = value;
		}

		public object GetAt(int index)
		{
			var objs = RawValue as object[];
			return objs[index];
		}

		public object GetAt(int index1, int index2)
		{
			var objs = RawValue as object[];
			var tmp = objs[index1] as object[];
			return tmp[index2];
		}

		public object GetMember(string name)
		{
			var table = RawValue as Dictionary<string, object>;
			return table[name];
		}

		public IEnumerable<object> GetAllValue()
		{
			var values = _GetAllValue(RawValue);
			return values;
		}

		#region private methods

		private static IEnumerable<object> _GetAllValue(object arg)
		{
			if (arg is object[])
			{
				var objs = (object[])arg;
				if (objs.Length > 0 && objs[0] is char)
				{
					// Parse as string
					yield return ConvertUtil.CharArray2String(arg);
				}
				else if (objs.Length > 0 && objs[0] is object[])
				{
					// Parse as 2 dimension array
					foreach (var tmp in objs)
					{
						var tmpArray = (object[])tmp;
						foreach (var obj in tmpArray.SelectMany(o => _GetAllValue(o)))
						{
							yield return obj;
						}
					}
				}
				else
				{
					foreach (var obj in objs.SelectMany(o => _GetAllValue(o)))
					{
						yield return obj;
					}
				}
			}
			else if (arg is Dictionary<string, object>)
			{
				var objs = (Dictionary<string, object>)arg;
				foreach (var obj in objs.Values.SelectMany(o => _GetAllValue(o)))
				{
					yield return obj;
				}
			}
			else
			{
				yield return arg;
			}
		}

		#endregion // private methods
	}
}