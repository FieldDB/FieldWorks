## --------------------------------------------------------------------------------------------
## Copyright (C) 2009 SIL International. All rights reserved.
##
## Distributable under the terms of either the Common Public License or the
## GNU Lesser General Public License, as specified in the LICENSING.txt file.
##
## NVelocity template file
## This file is used by the FdoGenerate task to generate the source code from the XMI
## database model.
## --------------------------------------------------------------------------------------------
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.FDO.Infrastructure.Impl;
using StructureMap.Configuration.DSL;
using StructureMap.Pipeline;

namespace SIL.FieldWorks.FDO.IOC
{
	/// <summary>
	/// Bootstrapper for FDO Common Service Locator.
	/// </summary>
	internal partial class FdoServiceLocatorFactory
	{
		private static void AddFactories(Registry registry)
		{
#foreach($module in $fdogenerate.Modules)
#foreach($class in $module.Classes)
#if ($class.Name == "LgWritingSystem")
#set( $classSfx = "FactoryFdo" )
#else
#set( $classSfx = "Factory" )
#end
#if(!$class.IsAbstract)
			registry
				.For<I${class.Name}$classSfx>()
				.LifecycleIs(new SingletonLifecycle())
				.Use<${class.Name}$classSfx>();
#end
#end
#end
		}

		private static void AddRepositories(Registry registry)
		{
#foreach($module in $fdogenerate.Modules)
#foreach($class in $module.Classes)
			registry
				.For<I${class.Name}Repository>()
				.LifecycleIs(new SingletonLifecycle())
				.Use<${class.Name}Repository>();
#end
#end
		}
	}
}
