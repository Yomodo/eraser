/* 
 * $Id$
 * Copyright 2008 The Eraser Project
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

#include "stdafx.h"

fNtQuerySystemInformation NtQuerySystemInformation = NULL;
fNtQueryInformationFile NtQueryInformationFile = NULL;
fNtQueryObject NtQueryObject = NULL;

class DllLoader
{
public:
	DllLoader()
	{
		HINSTANCE ntDll = LoadLibrary(L"NtDll.dll");
		NtQuerySystemInformation = reinterpret_cast<fNtQuerySystemInformation>(
			GetProcAddress(ntDll, "NtQuerySystemInformation"));
		NtQueryInformationFile = reinterpret_cast<fNtQueryInformationFile>(
			GetProcAddress(ntDll, "NtQueryInformationFile"));
		NtQueryObject = reinterpret_cast<fNtQueryObject>(
			GetProcAddress(ntDll, "NtQueryObject"));
	}

};

DllLoader loader;