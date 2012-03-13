﻿/* 
 * $Id: ProgressManager.cs 2406 2012-01-12 05:19:39Z lowjoel $
 * Copyright 2008-2012 The Eraser Project
 * Original Author: Joel Low <lowjoel@users.sourceforge.net>
 * Modified By: 
 * 
 * This file is part of Eraser.
 * 
 * Eraser is free software: you can redistribute it and/or modify it under the
 * terms of the GNU General Public License as published by the Free Software
 * Foundation, either version 3 of the License, or (at your option) any later
 * version.
 * 
 * Eraser is distributed in the hope that it will be useful, but WITHOUT ANY
 * WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR
 * A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 * 
 * A copy of the GNU General Public License can be found at
 * <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Security.Permissions;

using Eraser.Util;
using Eraser.Plugins;
using Eraser.Plugins.Registrars;
using Eraser.Plugins.ExtensionPoints;

namespace Eraser.DefaultPlugins
{
	abstract class ErasureTargetBase : IErasureTarget
	{
		#region IErasureTarget Members

		public abstract string Name
		{
			get;
		}

		public abstract bool SupportsMethod(IErasureMethod method);

		public abstract IErasureTargetConfigurer Configurer
		{
			get;
		}

		public abstract void Execute();

		#endregion

		#region Serialization code
		protected ErasureTargetBase(SerializationInfo info, StreamingContext context)
		{
			Guid methodGuid = (Guid)info.GetValue("Method", typeof(Guid));
			if (methodGuid == Guid.Empty)
				Method = ErasureMethodRegistrar.Default;
			else
				Method = Host.Instance.ErasureMethods[methodGuid];
		}

		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("Method", Method.Guid);
		}
		#endregion

		#region IRegisterable Members

		public abstract Guid Guid
		{
			get;
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		protected ErasureTargetBase()
		{
			Method = ErasureMethodRegistrar.Default;
		}

		public IErasureMethod Method
		{
			get{
				return method;
			}
			set
			{
				if (!SupportsMethod(value))
					throw new ArgumentException(S._("The selected erasure method is not " +
						"supported for this erasure target."));
				method = value;
			}
		}

		/// <summary>
		/// Gets the effective erasure method for the current target (i.e., returns
		/// the correct erasure method for cases where the <see cref="Method"/>
		/// property is <see cref="ErasureMethodRegistrar.Default"/>
		/// </summary>
		/// <returns>The Erasure method which the target should be erased with.
		/// This function will never return <see cref="ErasureMethodRegistrar.Default"/></returns>
		public virtual IErasureMethod EffectiveMethod
		{
			get
			{
				if (Method != ErasureMethodRegistrar.Default)
					return Method;

				throw new InvalidOperationException("The effective method of the erasure " +
					"target cannot be ErasureMethodRegistrar.Default");
			}
		}
		
		
		/// <summary>
		/// The backing variable for the <see cref="Method"/> property.
		/// </summary>
		private IErasureMethod method;
	}
}