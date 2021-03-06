/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: GrAppData.h
Responsibility: Sharon Correll
Last reviewed: not yet

Description:
	FieldWorks-only version of data structures and data types that are needed by applications
	that use the Graphite engine.

	Right now this is the ITextSource.h file.
----------------------------------------------------------------------------------------------*/

#include "GrResult.h"
typedef unsigned short int utf16;	// UTF16 encoded unicode codepoints.
typedef unsigned int featid;		// font feature IDs
typedef unsigned int lgid;			// language ID (for access feature UI strings)
typedef unsigned int toffset;		// text-source index

typedef struct {		// ISO-639-3 language code (for mapping onto features)
	char rgch[4];
} isocode;

// Ideally we should include GrPlatform.h or GrData.h here, but then we get weird errors with
// min and max (since GrPlatform.h fiddles with those definitions)
