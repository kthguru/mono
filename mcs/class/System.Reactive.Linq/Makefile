thisdir = class/System.Reactive.Linq
SUBDIRS = 
include ../../build/rules.make

LIBRARY = System.Reactive.Linq.dll
LIB_MCS_FLAGS = \
		@more_build_args \
	        -r:System.dll \
	        -r:System.Core.dll \
		-r:System.Reactive.Interfaces.dll \
		-r:System.Reactive.Core.dll

ifeq (true, $(GENERATE_RESOURCES))
LIB_MCS_FLAGS += /define:GENERATING_RESOURCES
endif

RESX_RESOURCES = \
	Strings_Linq.resources

CLEAN_FILES += $(RESX_RESOURCES)

RESOURCES = $(RESX_RESOURCES)

PREBUILT = $(RESX_RESOURCES:=.prebuilt)

ifeq (2.1, $(FRAMEWORK_VERSION))
LIB_MCS_FLAGS += -d:NO_TASK_DELAY
endif

NET_4_5 := $(filter 4.5, $(FRAMEWORK_VERSION))
ifdef NET_4_5
LIB_MCS_FLAGS += -d:HAS_EDI -d:PREFERASYNC -d:PREFER_ASYNC
endif

TEST_MCS_FLAGS = $(LIB_MCS_FLAGS)

EXTRA_DISTFILES = more_build_args $(RESX_RESOURCES:.resources=.resx) $(PREBUILT)

VALID_PROFILE := $(filter 2 4, $(FRAMEWORK_VERSION_MAJOR))
ifndef VALID_PROFILE
LIBRARY_NAME = dummy-System.System.Reactive.Linq.dll
NO_SIGN_ASSEMBLY = yes
endif

NO_INSTALL = yes
NO_TEST = yes

include ../../build/library.make

$(the_lib): $(RESOURCES)

$(RESX_RESOURCES): %.resources: %.resx
	$(RESGEN) $< || cp $@.prebuilt $@

$(PREBUILT): %.prebuilt: %
	cp $* $@

dist-default: $(PREBUILT)

