
#	$Id: Makefile 2650 2009-12-15 14:15:18Z beilharz $
#
#	Makefile for FW Language directory
#
#	Neil Mayhew - 2007-03-15
#
#	Adapted from Language.mak


BUILD_ROOT = ../..
include $(BUILD_ROOT)/Bld/_names.mak
BUILD_PRODUCT = $(LANGUAGE_NAME)
include $(BUILD_ROOT)/Bld/_init.mak.lnx

DEFINES := $(DEFINES) -DGR_FW -DSUPPRESS_FW_EXCEPTION_HANDLING -DWIN32_KEY_VALUES=1

ifeq ($(BUILD_CONFIG),Debug)
	DEBUG_LIBS = $(OUT_DIR)/libDebugProcs.a
	DEFINES  := $(DEFINES) -D_DEBUG
	OPTIMIZATIONS = -O0
else
	OPTIMIZATIONS = -O3
endif

CELLAR_SRC = $(SRC)/Cellar
GRAPHITE_SRC = $(SRC)/Graphite
GRFW_SRC = $(GRAPHITE_SRC)/FwOnly
GRUTIL_LIB = $(GRAPHITE_SRC)/lib
TTFUTIL_LIB = $(GRAPHITE_SRC)/TtfUtil
VIEWS_LIB = $(SRC)/views/lib

PACKAGES = glib-2.0 gtk+-2.0 glibmm-2.4 gtkmm-2.4 cairomm-1.0 pangomm-1.4

# Make sure Lang is first, as we want to get the Main.h from there,
# not any of the others (e.g., in Views)
INCLUDES := -I$(LANGUAGE_SRC) -I$(GENERIC_SRC) -I$(APPCORE_SRC) -I$(TEXT_SRC)
INCLUDES := $(INCLUDES) -I$(KERNEL_SRC) -I$(CELLAR_SRC) -I$(DBACCESS_SRC)
INCLUDES := $(INCLUDES) -I$(GRUTIL_LIB) -I$(TTFUTIL_LIB) -I$(VIEWS_SRC) -I$(VIEWS_LIB) -I$(GRFW_SRC)
INCLUDES := $(shell icu-config --cppflags) \
	$(INCLUDES) \
	-I$(FWINCLUDE) \
	-I$(COM_OUT_DIR) \
	-I$(WIN32MORE_INC) \
	-I$(COM_INC) \
	-I$(WIN32BASE_INC) \
	$(shell pkg-config --cflags $(PACKAGES)) \


LDLIBS = \
	-L$(FWINCLUDE) \
	$(DEBUG_LIBS) \
	-L$(WIN32MORE_LIB) -lWin32More \
	-L$(COM_LIB) -lcom \
	-L$(WIN32BASE_LIB) -lWin32Base \
	$(shell pkg-config --libs $(PACKAGES)) \
	$(shell icu-config --ldflags) \
	-luuid -lexpat

CPPFLAGS = $(DEFINES) $(INCLUDES) -MMD
CXXFLAGS = -g $(OPTIMIZATIONS) -fPIC -fvisibility=hidden -Werror -Waddress -Warray-bounds -Wmissing-field-initializers -Wclobbered -Wreturn-type -fstack-protector-all -Wshadow -rdynamic

ARFLAGS  = -crs

# XML_INC = $(CELLAR_XML) -I$(LANG_XML)

#PATH = $(COM_OUT_DIR);$(PATH)

RCFILE = Language.rc
DEFFILE = Language.def

LINK_LIBS := \
	$(DEBUG_LIBS) \
	$(OUT_DIR)/libGeneric.a \
	$(COM_OUT_DIR)/libFwKernelTlb.a \
	$(COM_OUT_DIR)/libGraphiteTlb.a \
	$(COM_OUT_DIR)/libViewsTlb.a \

# === Object Lists ===

OBJ_LANGSERV =

# not currently used
OBJ_LANMODEL = \
	$(INT_DIR)/LgCharOverride.o \
	$(INT_DIR)/LgCharPropSpec.o \
	$(INT_DIR)/LgCharSpec.o \
	$(INT_DIR)/LgCollaterSpec.o \
	$(INT_DIR)/LgComponent.o \
	$(INT_DIR)/LgConverterSpec.o \
	$(INT_DIR)/LgConvertStringSpec.o \
	$(INT_DIR)/LgExternalSpecComponent.o \
	$(INT_DIR)/LgLineBreakSpaceSpec.o \
	$(INT_DIR)/LgLineBreakSpec.o \
	$(INT_DIR)/LgOwnedSpecComponent.o \
	$(INT_DIR)/LgNumericConverterSpec.o \
	$(INT_DIR)/LgRenderSpec.o \
	$(INT_DIR)/LgRomanRendererSpec.o \
	$(INT_DIR)/LgSpec.o \
	$(INT_DIR)/LgSpellCheckSpec.o \
	$(INT_DIR)/LgSysCollaterSpec.o \
	$(INT_DIR)/LgTokenizerSpec.o \
	$(INT_DIR)/LgUnicodeCollaterSpec.o \
	$(INT_DIR)/LgUserClassSpec.o \
	$(INT_DIR)/LgWfiCheckerSpec.o \
	$(INT_DIR)/LgWinRendSpec.o \
	$(INT_DIR)/LgWordBreakSpaceSpec.o \
	$(INT_DIR)/LgCPWordTokenizer.o \
	$(INT_DIR)/LgNumericEngine.o \
	$(INT_DIR)/TsStringPlus.o \
	$(INT_DIR)/FwXml.o \

OBJ_LANG = \
	$(INT_DIR)/LanguageGlobals.o \
	$(INT_DIR)/LgIcuWrappers.o \
	$(INT_DIR)/LgSimpleEngines.o \
	$(INT_DIR)/LgIcuCharPropEngine.o \
	$(INT_DIR)/LgFontManager.o \
	$(INT_DIR)/LgUnicodeCollater.o \
	$(INT_DIR)/LgInputMethodEditor.o \
	$(INT_DIR)/LocaleIndex.o \
	$(INT_DIR)/LgCodePageEnumerator.o \
	$(INT_DIR)/LgKeymanHandler.o \
	$(INT_DIR)/FwXml.o \
	$(INT_DIR)/WriteXml.o \
	$(INT_DIR)/FwStyledText.o \
	$(INT_DIR)/UniscribeLinux.o \
	$(INT_DIR)/UniscribeSegment.o \
	$(INT_DIR)/UniscribeEngine.o \
	$(INT_DIR)/RomRenderEngine.o \
	$(INT_DIR)/RomRenderSegment.o \


#	LgCodePageEnumerator.o \  # This is used only in FwCoreDlgs (written in C#) - Need by FwCoreDlgs as loaded as a COM object
#	LgKeymanHandler.o \
#	UniscribeEngine.o \
#	UniscribeSegment.o \

# Not used
XMO_LANGSERV = \
	$(COM_INT_DIR)/LgCharOverride.xmo \
	$(COM_INT_DIR)/LgCharPropSpec.xmo \
	$(COM_INT_DIR)/LgCharSpec.xmo \
	$(COM_INT_DIR)/LgCollaterSpec.xmo \
	$(COM_INT_DIR)/LgComponent.xmo \
	$(COM_INT_DIR)/LgConverterSpec.xmo \
	$(COM_INT_DIR)/LgConvertStringSpec.xmo \
	$(COM_INT_DIR)/LgWritingSystemModel.xmo \
	$(COM_INT_DIR)/LgExternalSpecComponent.xmo \
	$(COM_INT_DIR)/LgLineBreakSpaceSpec.xmo \
	$(COM_INT_DIR)/LgLineBreakSpec.xmo \
	$(COM_INT_DIR)/LgOwnedSpecComponent.xmo \
	$(COM_INT_DIR)/LgNumericConverterSpec.xmo \
	$(COM_INT_DIR)/LgRenderSpec.xmo \
	$(COM_INT_DIR)/LgRomanRendererSpec.xmo \
	$(COM_INT_DIR)/LgSpec.xmo \
	$(COM_INT_DIR)/LgSpellCheckSpec.xmo \
	$(COM_INT_DIR)/LgSysCollaterSpec.xmo \
	$(COM_INT_DIR)/LgTokenizerSpec.xmo \
	$(COM_INT_DIR)/LgUnicodeCollaterSpec.xmo \
	$(COM_INT_DIR)/LgUserClassSpec.xmo \
	$(COM_INT_DIR)/LgWfiCheckerSpec.xmo \
	$(COM_INT_DIR)/LgWinRendSpec.xmo \
	$(COM_INT_DIR)/LgWordBreakSpaceSpec.xmo \

OBJ_OTHER = \
	$(INT_DIR)/ModuleEntry.o \
	$(INT_DIR)/TextProps1.o \

OBJ_LIB = \
	$(INT_DIR)/Language_GUIDs.o \
	$(INT_DIR)/LanguageExtra_GUIDs.o

#OBJ_GRUTIL = \
#	$(GRUTIL_LIB)/GrUtil.o \
#	$(TTFUTIL_LIB)/TtfUtil.o \

# XMO_ALL = $(XMO_LANGSERV)
#
# IDL_MAIN = $(COM_OUT_DIR)/LanguageTlb.idl
#
# PS_MAIN = LanguagePs
#
# OBJECTS_IDH = $(COM_INT_DIR)/Objects.idh
#
# ? OBJECTS_H = $(COM_INT_DIR)/Objects.h

OBJ_ALL = $(OBJ_LANG) $(OBJ_LANGSERV) $(OBJ_OTHER) $(OBJ_GRUTIL)

# === Targets ===

all: $(OUT_DIR)/libLanguage.so $(COM_OUT_DIR)/libLanguageTlb.a ComponentsMap link_check

$(OUT_DIR)/libLanguage.so: $(OBJ_ALL) $(LINK_LIBS) $(COM_OUT_DIR)/libLanguageTlb.a
ifeq "$(GCC46)" "1"
	$(LINK.cc) -shared -o $@ -Wl,-whole-archive $(COM_OUT_DIR)/libLanguageTlb.a -Wl,-no-whole-archive $(OBJ_ALL) $(LINK_LIBS) $(LDLIBS)
else
	$(LINK.cc) -shared -o $@ $^ $(LDLIBS)
endif

$(COM_OUT_DIR)/libLanguageTlb.a: $(OBJ_LIB)
	$(AR) $(ARFLAGS) $@ $^

link_check: $(INT_DIR)/libLanguage

$(INT_DIR)/libLanguage: $(OBJ_ALL) $(GENERIC_OBJ)/main.o $(LINK_LIBS) $(COM_OUT_DIR)/libLanguageTlb.a
	$(LINK.cc) -o $@ -Wl,-whole-archive $(COM_OUT_DIR)/libLanguageTlb.a -Wl,-no-whole-archive $(GENERIC_OBJ)/main.o $(OBJ_ALL) $(LINK_LIBS) $(LDLIBS)

ComponentsMap: $(OUT_DIR)/libLanguage.so
	(export PATH=$(PATH):$(COM_DIR)/build$(ARCH)/bin && cd $(OUT_DIR) && generate-map.sh libLanguage.so > libLanguage.compmap)

# === Rules ===

PCHNAME = $(INT_DIR)/Main.h.gch

$(OBJ_LANG) $(OBJ_LANGSERV): $(PCHNAME)

# ensure object dependies are built.

$(INT_DIR)/%.o: $(APPCORE_SRC)/%.cpp
	$(CXX) $(CXXFLAGS) $(CPPFLAGS) $(INCLUDES) $(TARGET_ARCH) \
	  -c $< -o $@ -MMD -MF $(@:%.o=%.d)

$(INT_DIR)/%.o: $(CELLAR_SRC)/%.cpp
	$(CXX) $(CXXFLAGS) $(CPPFLAGS) $(INCLUDES) $(TARGET_ARCH) \
	  -c $< -o $@ -MMD -MF $(@:%.o=%.d)

$(INT_DIR)/%.o: $(GENERIC_SRC)/%.cpp
	$(CXX) $(CXXFLAGS) $(CPPFLAGS) $(INCLUDES) $(TARGET_ARCH) \
	  -c $< -o $@ -MMD -MF $(@:%.o=%.d)

$(OUT_DIR)/libDebugProcs.a:
	@$(MAKE) -C $(SRC)/DebugProcs $@ -q || \
	 $(MAKE) -C $(SRC)/DebugProcs $@

$(OUT_DIR)/libGeneric.a:
	@$(MAKE) -C $(SRC)/Generic $@ -q || \
	 $(MAKE) -C $(SRC)/Generic $@

$(COM_OUT_DIR)/libFwKernelTlb.a:
	@$(MAKE) -C $(SRC)/Kernel $@ -q || \
	 $(MAKE) -C $(SRC)/Kernel $@

$(COM_OUT_DIR)/libGraphiteTlb.a:
	@$(MAKE) -C $(GRENGINE_SRC) all -q || \
	 $(MAKE) -C $(GRENGINE_SRC) all

$(COM_OUT_DIR)/libViewsTlb.a:
	@$(MAKE) -C $(SRC)/views/ $@ -q || \
	 $(MAKE) -C $(SRC)/views/ $@

%.h.gch: %.h
	$(COMPILE.cc) -o $@ $<

-include $(OBJ_ALL:%.o=%.d)
-include $(PCHNAME:%.gch=%.d)

clean:
	$(RM) $(OUT_DIR)/libLanguage.so $(COM_OUT_DIR)/libLanguageTlb.a $(INT_DIR)/libLanguage $(INT_DIR)/*.[od] $(INT_DIR)/*.gch
