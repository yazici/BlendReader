﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Collections.ObjectModel;

namespace Blender
{
	/// <summary>
	/// This class represents a .blend structure type
	/// </summary>
	public class BlendStructureType : IBlendType
	{
		#region public types

		public static readonly int InvalidSdnaIndex = -1;

		public struct MemberDecl
		{
			public string Name;
			public IBlendType Type;

			public MemberDecl(string name, IBlendType type)
			{
				Name = name;
				Type = type;
			}
		}

		#endregion // public types

		#region properties

		private int m_sdnaIndex;
		public int SdnaIndex
		{
			get
			{
				return m_sdnaIndex;
			}
		}

		private MemberDecl[] m_decls;
		public ReadOnlyCollection<MemberDecl> MemberDecls
		{
			get
			{
				if (HasMemberDecls)
				{
					return m_decls.ToList().AsReadOnly();
				}
				else
				{
					return ListUtil.CreateReadOnlyCollection<MemberDecl>();
				}
			}
		}

		/// <summary>
		/// get whether this has member declaration
		/// </summary>
		/// <remarks>
		/// no-member blend structure type is considered as imperfect.
		/// do NOT call ReadValue(), SizeOf() for it. otherwise these methods throw an exception.
		/// you can use SetMemberDecls() to make a perfect type.
		/// this is a mechanism for recursive structure.
		/// </remarks>
		public bool HasMemberDecls
		{
			get
			{
				return m_decls != null;
			}
		}
		
		#endregion // properties

		public BlendStructureType(string name, MemberDecl[] decls)
		{
			m_name = name;
			m_sdnaIndex = InvalidSdnaIndex;
			if (decls != null)
			{
				SetMemberDecls(decls);
			}
		}

		public BlendStructureType(string name, int sdnaIndex, MemberDecl[] decls)
		{
			m_name = name;
			
			m_sdnaIndex = sdnaIndex;
			if (decls != null)
			{
				SetMemberDecls(decls);
			}
		}

		/// <summary>
		/// get size of this type
		/// </summary>
		/// <returns>size [byte]</returns>
		public int SizeOf()
		{
			int size = 0;
			foreach (var decl in m_decls)
			{
				size += decl.Type.SizeOf();
			}

			return size;
		}

		/// <summary>
		/// Read value corresponded this type from binary
		/// </summary>
		/// <param name="context">variable for making a value</param>
		/// <returns>value</returns>
		/// <seealso cref="IBlendType.ReadValue"/>
		public BlendValueCapsule ReadValue(ReadValueContext context)
		{
			var rawValue = new _RawValue();

			foreach (var decl in m_decls)
			{
				var value = decl.Type.ReadValue(context);
				rawValue.Add(decl.Name, value);
			}

			return new _BlendValueCapsule(this, rawValue);
		}

		public static BlendValueCapsule GetMemberValue(BlendValueCapsule value, string name)
		{
			Debug.Assert(value.Type.GetType() == typeof(BlendStructureType), "tyep unmatched");
			var rawValue = value.GetRawValue() as _RawValue;
			return rawValue[name];
		}

		public static IEnumerable<object> GetAllRawValue(BlendValueCapsule value)
		{
			Debug.Assert(value.Type.GetType() == typeof(BlendStructureType), "tyep unmatched");
			var rawValue = value.GetRawValue() as _RawValue;
			foreach (var memberValue in rawValue.Values.SelectMany(v => v.GetAllValue()))
			{
				yield return memberValue;
			}
		}

		/// <summary>
		/// get a name of type
		/// </summary>
		public string Name
		{
			get
			{
				return m_name;
			}
		}

		public void SetMemberDecls(MemberDecl[] decls)
		{
			Debug.Assert(!HasMemberDecls, "member decls already added");			

			if (decls != null)
			{
				m_decls = new MemberDecl[decls.Length];
				decls.CopyTo(m_decls, 0);
			}
		}

		public override String ToString()
		{
			return m_name;
		}

		public bool Equals(IBlendType type)
		{
			var structure = type as BlendStructureType;
			if (structure == null)
			{
				// type unmatched
				return false;
			}

			return Name == structure.Name;
		}

		public static BlendStructureType CreateImperfect(string name, int sdnaIndex)
		{
			return new BlendStructureType(name, sdnaIndex, null);
		}

		#region private types

		private class _RawValue : Dictionary<string, BlendValueCapsule> { }

		private class _BlendValueCapsule : BlendValueCapsule
		{
			public _BlendValueCapsule(IBlendType type, object value) : base(type, value) {}
		
			override public BlendValueCapsule GetMember(string name)
			{
				return BlendStructureType.GetMemberValue(this, name);
			}

			override public IEnumerable<object> GetAllValue()
			{
				return BlendStructureType.GetAllRawValue(this);
			}
		}

		#endregion // private types

		#region private members

		private string m_name;

		#endregion // private members
	}
}
