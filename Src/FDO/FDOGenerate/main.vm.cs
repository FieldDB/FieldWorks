## --------------------------------------------------------------------------------------------
## Copyright (C) 2006-2009 SIL International. All rights reserved.
##
## Distributable under the terms of either the Common Public License or the
## GNU Lesser General Public License, as specified in the LICENSING.txt file.
##
## NVelocity template file
## This file is used by the FdoGenerate task to generate the source code from the XMI
## database model.
## --------------------------------------------------------------------------------------------
#set( $generated = "_Generated")
//This is automatically generated by FDOGenerate task.  ****Do not edit****
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml; // XMLWriter
using System.Xml.Linq;
using System.Linq;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.CoreImpl;
using SIL.Utils;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.FDO.DomainImpl
{
#foreach($module in $fdogenerate.Modules)
#parse("module.vm.cs")
#end
}
## Generate constants
$fdogenerate.SetOutput("GeneratedFdoConstants.cs")
$fdogenerate.Process("FdoConstants.vm.cs")
## Generate interfaces
$fdogenerate.SetOutput("GeneratedFdoInterfaces.cs")
$fdogenerate.Process("Interfaces.vm.cs")
## Generate factory interfaces
$fdogenerate.SetOutput("GeneratedFactoryInterfaces.cs")
$fdogenerate.Process("FactoryInterfaces.vm.cs")
## Generate Factory Implementations
$fdogenerate.SetOutput("DomainImpl/GeneratedFactoryImplementations.cs")
$fdogenerate.Process("FactoryImplementations.vm.cs")
## Generate Repository interfaces
$fdogenerate.SetOutput("GeneratedRepositoryInterfaces.cs")
$fdogenerate.Process("RepositoryInterfaces.vm.cs")
## Generate Repository Implementations
$fdogenerate.SetOutput("Infrastructure/Impl/GeneratedRepositoryImplementations.cs")
$fdogenerate.Process("RepositoryImplementations.vm.cs")
## Generate partial FDOBackendProvoider class to get the model verion number from the master model xml file
$fdogenerate.SetOutput("Infrastructure/Impl/GeneratedFDOBackendProvider.cs")
$fdogenerate.Process("FDOBackendProvider.vm.cs")
## Generate bootstrapper code
$fdogenerate.SetOutput("IOC/GeneratedServiceLocatorBootstrapper.cs")
$fdogenerate.Process("FdoServiceLocatorBootstrapper.vm.cs")
