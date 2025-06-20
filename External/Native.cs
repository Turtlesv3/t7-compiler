﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Evasion.ModuleMapper.ModuleConst;
using static System.EnvironmentEx;

namespace System
{
    public static class Native
    {
        #region typedef
        #region struct
        [StructLayout(LayoutKind.Sequential)]
        public struct LIST_ENTRY
        {
            public IntPtr Flink;
            public IntPtr Blink;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct UNICODE_STRING
        {
            public UInt16 Length;
            public UInt16 MaximumLength;
            public IntPtr Buffer;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ANSI_STRING
        {
            public UInt16 Length;
            public UInt16 MaximumLength;
            public IntPtr Buffer;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct OSVERSIONINFOEX
        {
            public uint OSVersionInfoSize;
            public uint MajorVersion;
            public uint MinorVersion;
            public uint BuildNumber;
            public uint PlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string CSDVersion;
            public ushort ServicePackMajor;
            public ushort ServicePackMinor;
            public ushort SuiteMask;
            public byte ProductType;
            public byte Reserved;
        }

        public struct PROCESS_BASIC_INFORMATION
        {
            public IntPtr ExitStatus;
            public IntPtr PebBaseAddress;
            public IntPtr AffinityMask;
            public IntPtr BasePriority;
            public UIntPtr UniqueProcessId;
            public int InheritedFromUniqueProcessId;

            public int Size
            {
                get { return (int)Marshal.SizeOf(typeof(PROCESS_BASIC_INFORMATION)); }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct IMAGE_IMPORT_DESCRIPTOR
        {
            public uint OriginalFirstThunk;
            public uint TimeDateStamp;
            public uint ForwarderChain;
            public uint Name;
            public uint FirstThunk;
        }
        #endregion

        #region enum
        [Flags]
        public enum AllocationType : uint
        {
            Commit = 0x1000,
            Reserve = 0x2000,
            Decommit = 0x4000,
            Release = 0x8000,
            Reset = 0x80000,
            Physical = 0x400000,
            TopDown = 0x100000,
            WriteWatch = 0x200000,
            ResetUndo = 0x1000000,
            LargePages = 0x20000000
        }

        [Flags]
        public enum MemoryProtection
        {
            Execute = 0x10,
            ExecuteRead = 0x20,
            ExecuteReadWrite = 0x40,
            ExecuteWriteCopy = 0x80,
            NoAccess = 0x01,
            ReadOnly = 0x02,
            ReadWrite = 0x04,
            WriteCopy = 0x08,
            GuardModifierflag = 0x100,
            NoCacheModifierflag = 0x200,
            WriteCombineModifierflag = 0x400
        }

        /// <summary>
        /// NTSTATUS is an undocument enum. https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-erref/596a1078-e883-4972-9bbc-49e60bebca55
        /// https://www.pinvoke.net/default.aspx/Enums/NtStatus.html
        /// </summary>
        public enum NTSTATUS : uint
        {
            // Success
            Success = 0x00000000,
            Wait0 = 0x00000000,
            Wait1 = 0x00000001,
            Wait2 = 0x00000002,
            Wait3 = 0x00000003,
            Wait63 = 0x0000003f,
            Abandoned = 0x00000080,
            AbandonedWait0 = 0x00000080,
            AbandonedWait1 = 0x00000081,
            AbandonedWait2 = 0x00000082,
            AbandonedWait3 = 0x00000083,
            AbandonedWait63 = 0x000000bf,
            UserApc = 0x000000c0,
            KernelApc = 0x00000100,
            Alerted = 0x00000101,
            Timeout = 0x00000102,
            Pending = 0x00000103,
            Reparse = 0x00000104,
            MoreEntries = 0x00000105,
            NotAllAssigned = 0x00000106,
            SomeNotMapped = 0x00000107,
            OpLockBreakInProgress = 0x00000108,
            VolumeMounted = 0x00000109,
            RxActCommitted = 0x0000010a,
            NotifyCleanup = 0x0000010b,
            NotifyEnumDir = 0x0000010c,
            NoQuotasForAccount = 0x0000010d,
            PrimaryTransportConnectFailed = 0x0000010e,
            PageFaultTransition = 0x00000110,
            PageFaultDemandZero = 0x00000111,
            PageFaultCopyOnWrite = 0x00000112,
            PageFaultGuardPage = 0x00000113,
            PageFaultPagingFile = 0x00000114,
            CrashDump = 0x00000116,
            ReparseObject = 0x00000118,
            NothingToTerminate = 0x00000122,
            ProcessNotInJob = 0x00000123,
            ProcessInJob = 0x00000124,
            ProcessCloned = 0x00000129,
            FileLockedWithOnlyReaders = 0x0000012a,
            FileLockedWithWriters = 0x0000012b,

            // Informational
            Informational = 0x40000000,
            ObjectNameExists = 0x40000000,
            ThreadWasSuspended = 0x40000001,
            WorkingSetLimitRange = 0x40000002,
            ImageNotAtBase = 0x40000003,
            RegistryRecovered = 0x40000009,

            // Warning
            Warning = 0x80000000,
            GuardPageViolation = 0x80000001,
            DatatypeMisalignment = 0x80000002,
            Breakpoint = 0x80000003,
            SingleStep = 0x80000004,
            BufferOverflow = 0x80000005,
            NoMoreFiles = 0x80000006,
            HandlesClosed = 0x8000000a,
            PartialCopy = 0x8000000d,
            DeviceBusy = 0x80000011,
            InvalidEaName = 0x80000013,
            EaListInconsistent = 0x80000014,
            NoMoreEntries = 0x8000001a,
            LongJump = 0x80000026,
            DllMightBeInsecure = 0x8000002b,

            // Error
            Error = 0xc0000000,
            Unsuccessful = 0xc0000001,
            NotImplemented = 0xc0000002,
            InvalidInfoClass = 0xc0000003,
            InfoLengthMismatch = 0xc0000004,
            AccessViolation = 0xc0000005,
            InPageError = 0xc0000006,
            PagefileQuota = 0xc0000007,
            InvalidHandle = 0xc0000008,
            BadInitialStack = 0xc0000009,
            BadInitialPc = 0xc000000a,
            InvalidCid = 0xc000000b,
            TimerNotCanceled = 0xc000000c,
            InvalidParameter = 0xc000000d,
            NoSuchDevice = 0xc000000e,
            NoSuchFile = 0xc000000f,
            InvalidDeviceRequest = 0xc0000010,
            EndOfFile = 0xc0000011,
            WrongVolume = 0xc0000012,
            NoMediaInDevice = 0xc0000013,
            NoMemory = 0xc0000017,
            ConflictingAddresses = 0xc0000018,
            NotMappedView = 0xc0000019,
            UnableToFreeVm = 0xc000001a,
            UnableToDeleteSection = 0xc000001b,
            IllegalInstruction = 0xc000001d,
            AlreadyCommitted = 0xc0000021,
            AccessDenied = 0xc0000022,
            BufferTooSmall = 0xc0000023,
            ObjectTypeMismatch = 0xc0000024,
            NonContinuableException = 0xc0000025,
            BadStack = 0xc0000028,
            NotLocked = 0xc000002a,
            NotCommitted = 0xc000002d,
            InvalidParameterMix = 0xc0000030,
            ObjectNameInvalid = 0xc0000033,
            ObjectNameNotFound = 0xc0000034,
            ObjectNameCollision = 0xc0000035,
            ObjectPathInvalid = 0xc0000039,
            ObjectPathNotFound = 0xc000003a,
            ObjectPathSyntaxBad = 0xc000003b,
            DataOverrun = 0xc000003c,
            DataLate = 0xc000003d,
            DataError = 0xc000003e,
            CrcError = 0xc000003f,
            SectionTooBig = 0xc0000040,
            PortConnectionRefused = 0xc0000041,
            InvalidPortHandle = 0xc0000042,
            SharingViolation = 0xc0000043,
            QuotaExceeded = 0xc0000044,
            InvalidPageProtection = 0xc0000045,
            MutantNotOwned = 0xc0000046,
            SemaphoreLimitExceeded = 0xc0000047,
            PortAlreadySet = 0xc0000048,
            SectionNotImage = 0xc0000049,
            SuspendCountExceeded = 0xc000004a,
            ThreadIsTerminating = 0xc000004b,
            BadWorkingSetLimit = 0xc000004c,
            IncompatibleFileMap = 0xc000004d,
            SectionProtection = 0xc000004e,
            EasNotSupported = 0xc000004f,
            EaTooLarge = 0xc0000050,
            NonExistentEaEntry = 0xc0000051,
            NoEasOnFile = 0xc0000052,
            EaCorruptError = 0xc0000053,
            FileLockConflict = 0xc0000054,
            LockNotGranted = 0xc0000055,
            DeletePending = 0xc0000056,
            CtlFileNotSupported = 0xc0000057,
            UnknownRevision = 0xc0000058,
            RevisionMismatch = 0xc0000059,
            InvalidOwner = 0xc000005a,
            InvalidPrimaryGroup = 0xc000005b,
            NoImpersonationToken = 0xc000005c,
            CantDisableMandatory = 0xc000005d,
            NoLogonServers = 0xc000005e,
            NoSuchLogonSession = 0xc000005f,
            NoSuchPrivilege = 0xc0000060,
            PrivilegeNotHeld = 0xc0000061,
            InvalidAccountName = 0xc0000062,
            UserExists = 0xc0000063,
            NoSuchUser = 0xc0000064,
            GroupExists = 0xc0000065,
            NoSuchGroup = 0xc0000066,
            MemberInGroup = 0xc0000067,
            MemberNotInGroup = 0xc0000068,
            LastAdmin = 0xc0000069,
            WrongPassword = 0xc000006a,
            IllFormedPassword = 0xc000006b,
            PasswordRestriction = 0xc000006c,
            LogonFailure = 0xc000006d,
            AccountRestriction = 0xc000006e,
            InvalidLogonHours = 0xc000006f,
            InvalidWorkstation = 0xc0000070,
            PasswordExpired = 0xc0000071,
            AccountDisabled = 0xc0000072,
            NoneMapped = 0xc0000073,
            TooManyLuidsRequested = 0xc0000074,
            LuidsExhausted = 0xc0000075,
            InvalidSubAuthority = 0xc0000076,
            InvalidAcl = 0xc0000077,
            InvalidSid = 0xc0000078,
            InvalidSecurityDescr = 0xc0000079,
            ProcedureNotFound = 0xc000007a,
            InvalidImageFormat = 0xc000007b,
            NoToken = 0xc000007c,
            BadInheritanceAcl = 0xc000007d,
            RangeNotLocked = 0xc000007e,
            DiskFull = 0xc000007f,
            ServerDisabled = 0xc0000080,
            ServerNotDisabled = 0xc0000081,
            TooManyGuidsRequested = 0xc0000082,
            GuidsExhausted = 0xc0000083,
            InvalidIdAuthority = 0xc0000084,
            AgentsExhausted = 0xc0000085,
            InvalidVolumeLabel = 0xc0000086,
            SectionNotExtended = 0xc0000087,
            NotMappedData = 0xc0000088,
            ResourceDataNotFound = 0xc0000089,
            ResourceTypeNotFound = 0xc000008a,
            ResourceNameNotFound = 0xc000008b,
            ArrayBoundsExceeded = 0xc000008c,
            FloatDenormalOperand = 0xc000008d,
            FloatDivideByZero = 0xc000008e,
            FloatInexactResult = 0xc000008f,
            FloatInvalidOperation = 0xc0000090,
            FloatOverflow = 0xc0000091,
            FloatStackCheck = 0xc0000092,
            FloatUnderflow = 0xc0000093,
            IntegerDivideByZero = 0xc0000094,
            IntegerOverflow = 0xc0000095,
            PrivilegedInstruction = 0xc0000096,
            TooManyPagingFiles = 0xc0000097,
            FileInvalid = 0xc0000098,
            InsufficientResources = 0xc000009a,
            InstanceNotAvailable = 0xc00000ab,
            PipeNotAvailable = 0xc00000ac,
            InvalidPipeState = 0xc00000ad,
            PipeBusy = 0xc00000ae,
            IllegalFunction = 0xc00000af,
            PipeDisconnected = 0xc00000b0,
            PipeClosing = 0xc00000b1,
            PipeConnected = 0xc00000b2,
            PipeListening = 0xc00000b3,
            InvalidReadMode = 0xc00000b4,
            IoTimeout = 0xc00000b5,
            FileForcedClosed = 0xc00000b6,
            ProfilingNotStarted = 0xc00000b7,
            ProfilingNotStopped = 0xc00000b8,
            NotSameDevice = 0xc00000d4,
            FileRenamed = 0xc00000d5,
            CantWait = 0xc00000d8,
            PipeEmpty = 0xc00000d9,
            CantTerminateSelf = 0xc00000db,
            InternalError = 0xc00000e5,
            InvalidParameter1 = 0xc00000ef,
            InvalidParameter2 = 0xc00000f0,
            InvalidParameter3 = 0xc00000f1,
            InvalidParameter4 = 0xc00000f2,
            InvalidParameter5 = 0xc00000f3,
            InvalidParameter6 = 0xc00000f4,
            InvalidParameter7 = 0xc00000f5,
            InvalidParameter8 = 0xc00000f6,
            InvalidParameter9 = 0xc00000f7,
            InvalidParameter10 = 0xc00000f8,
            InvalidParameter11 = 0xc00000f9,
            InvalidParameter12 = 0xc00000fa,
            ProcessIsTerminating = 0xc000010a,
            MappedFileSizeZero = 0xc000011e,
            TooManyOpenedFiles = 0xc000011f,
            Cancelled = 0xc0000120,
            CannotDelete = 0xc0000121,
            InvalidComputerName = 0xc0000122,
            FileDeleted = 0xc0000123,
            SpecialAccount = 0xc0000124,
            SpecialGroup = 0xc0000125,
            SpecialUser = 0xc0000126,
            MembersPrimaryGroup = 0xc0000127,
            FileClosed = 0xc0000128,
            TooManyThreads = 0xc0000129,
            ThreadNotInProcess = 0xc000012a,
            TokenAlreadyInUse = 0xc000012b,
            PagefileQuotaExceeded = 0xc000012c,
            CommitmentLimit = 0xc000012d,
            InvalidImageLeFormat = 0xc000012e,
            InvalidImageNotMz = 0xc000012f,
            InvalidImageProtect = 0xc0000130,
            InvalidImageWin16 = 0xc0000131,
            LogonServer = 0xc0000132,
            DifferenceAtDc = 0xc0000133,
            SynchronizationRequired = 0xc0000134,
            DllNotFound = 0xc0000135,
            IoPrivilegeFailed = 0xc0000137,
            OrdinalNotFound = 0xc0000138,
            EntryPointNotFound = 0xc0000139,
            ControlCExit = 0xc000013a,
            InvalidAddress = 0xc0000141,
            PortNotSet = 0xc0000353,
            DebuggerInactive = 0xc0000354,
            CallbackBypass = 0xc0000503,
            PortClosed = 0xc0000700,
            MessageLost = 0xc0000701,
            InvalidMessage = 0xc0000702,
            RequestCanceled = 0xc0000703,
            RecursiveDispatch = 0xc0000704,
            LpcReceiveBufferExpected = 0xc0000705,
            LpcInvalidConnectionUsage = 0xc0000706,
            LpcRequestsNotAllowed = 0xc0000707,
            ResourceInUse = 0xc0000708,
            ProcessIsProtected = 0xc0000712,
            VolumeDirty = 0xc0000806,
            FileCheckedOut = 0xc0000901,
            CheckOutRequired = 0xc0000902,
            BadFileType = 0xc0000903,
            FileTooLarge = 0xc0000904,
            FormsAuthRequired = 0xc0000905,
            VirusInfected = 0xc0000906,
            VirusDeleted = 0xc0000907,
            TransactionalConflict = 0xc0190001,
            InvalidTransaction = 0xc0190002,
            TransactionNotActive = 0xc0190003,
            TmInitializationFailed = 0xc0190004,
            RmNotActive = 0xc0190005,
            RmMetadataCorrupt = 0xc0190006,
            TransactionNotJoined = 0xc0190007,
            DirectoryNotRm = 0xc0190008,
            CouldNotResizeLog = 0xc0190009,
            TransactionsUnsupportedRemote = 0xc019000a,
            LogResizeInvalidSize = 0xc019000b,
            RemoteFileVersionMismatch = 0xc019000c,
            CrmProtocolAlreadyExists = 0xc019000f,
            TransactionPropagationFailed = 0xc0190010,
            CrmProtocolNotFound = 0xc0190011,
            TransactionSuperiorExists = 0xc0190012,
            TransactionRequestNotValid = 0xc0190013,
            TransactionNotRequested = 0xc0190014,
            TransactionAlreadyAborted = 0xc0190015,
            TransactionAlreadyCommitted = 0xc0190016,
            TransactionInvalidMarshallBuffer = 0xc0190017,
            CurrentTransactionNotValid = 0xc0190018,
            LogGrowthFailed = 0xc0190019,
            ObjectNoLongerExists = 0xc0190021,
            StreamMiniversionNotFound = 0xc0190022,
            StreamMiniversionNotValid = 0xc0190023,
            MiniversionInaccessibleFromSpecifiedTransaction = 0xc0190024,
            CantOpenMiniversionWithModifyIntent = 0xc0190025,
            CantCreateMoreStreamMiniversions = 0xc0190026,
            HandleNoLongerValid = 0xc0190028,
            NoTxfMetadata = 0xc0190029,
            LogCorruptionDetected = 0xc0190030,
            CantRecoverWithHandleOpen = 0xc0190031,
            RmDisconnected = 0xc0190032,
            EnlistmentNotSuperior = 0xc0190033,
            RecoveryNotNeeded = 0xc0190034,
            RmAlreadyStarted = 0xc0190035,
            FileIdentityNotPersistent = 0xc0190036,
            CantBreakTransactionalDependency = 0xc0190037,
            CantCrossRmBoundary = 0xc0190038,
            TxfDirNotEmpty = 0xc0190039,
            IndoubtTransactionsExist = 0xc019003a,
            TmVolatile = 0xc019003b,
            RollbackTimerExpired = 0xc019003c,
            TxfAttributeCorrupt = 0xc019003d,
            EfsNotAllowedInTransaction = 0xc019003e,
            TransactionalOpenNotAllowed = 0xc019003f,
            TransactedMappingUnsupportedRemote = 0xc0190040,
            TxfMetadataAlreadyPresent = 0xc0190041,
            TransactionScopeCallbacksNotSet = 0xc0190042,
            TransactionRequiredPromotion = 0xc0190043,
            CannotExecuteFileInTransaction = 0xc0190044,
            TransactionsNotFrozen = 0xc0190045,

            MaximumNtStatus = 0xffffffff
        }

        public enum PROCESSINFOCLASS : int
        {
            ProcessBasicInformation = 0, // 0, q: PROCESS_BASIC_INFORMATION, PROCESS_EXTENDED_BASIC_INFORMATION
            ProcessQuotaLimits, // qs: QUOTA_LIMITS, QUOTA_LIMITS_EX
            ProcessIoCounters, // q: IO_COUNTERS
            ProcessVmCounters, // q: VM_COUNTERS, VM_COUNTERS_EX
            ProcessTimes, // q: KERNEL_USER_TIMES
            ProcessBasePriority, // s: KPRIORITY
            ProcessRaisePriority, // s: ULONG
            ProcessDebugPort, // q: HANDLE
            ProcessExceptionPort, // s: HANDLE
            ProcessAccessToken, // s: PROCESS_ACCESS_TOKEN
            ProcessLdtInformation, // 10
            ProcessLdtSize,
            ProcessDefaultHardErrorMode, // qs: ULONG
            ProcessIoPortHandlers, // (kernel-mode only)
            ProcessPooledUsageAndLimits, // q: POOLED_USAGE_AND_LIMITS
            ProcessWorkingSetWatch, // q: PROCESS_WS_WATCH_INFORMATION[]; s: void
            ProcessUserModeIOPL,
            ProcessEnableAlignmentFaultFixup, // s: BOOLEAN
            ProcessPriorityClass, // qs: PROCESS_PRIORITY_CLASS
            ProcessWx86Information,
            ProcessHandleCount, // 20, q: ULONG, PROCESS_HANDLE_INFORMATION
            ProcessAffinityMask, // s: KAFFINITY
            ProcessPriorityBoost, // qs: ULONG
            ProcessDeviceMap, // qs: PROCESS_DEVICEMAP_INFORMATION, PROCESS_DEVICEMAP_INFORMATION_EX
            ProcessSessionInformation, // q: PROCESS_SESSION_INFORMATION
            ProcessForegroundInformation, // s: PROCESS_FOREGROUND_BACKGROUND
            ProcessWow64Information, // q: ULONG_PTR
            ProcessImageFileName, // q: UNICODE_STRING
            ProcessLUIDDeviceMapsEnabled, // q: ULONG
            ProcessBreakOnTermination, // qs: ULONG
            ProcessDebugObjectHandle, // 30, q: HANDLE
            ProcessDebugFlags, // qs: ULONG
            ProcessHandleTracing, // q: PROCESS_HANDLE_TRACING_QUERY; s: size 0 disables, otherwise enables
            ProcessIoPriority, // qs: ULONG
            ProcessExecuteFlags, // qs: ULONG
            ProcessResourceManagement,
            ProcessCookie, // q: ULONG
            ProcessImageInformation, // q: SECTION_IMAGE_INFORMATION
            ProcessCycleTime, // q: PROCESS_CYCLE_TIME_INFORMATION
            ProcessPagePriority, // q: ULONG
            ProcessInstrumentationCallback, // 40
            ProcessThreadStackAllocation, // s: PROCESS_STACK_ALLOCATION_INFORMATION, PROCESS_STACK_ALLOCATION_INFORMATION_EX
            ProcessWorkingSetWatchEx, // q: PROCESS_WS_WATCH_INFORMATION_EX[]
            ProcessImageFileNameWin32, // q: UNICODE_STRING
            ProcessImageFileMapping, // q: HANDLE (input)
            ProcessAffinityUpdateMode, // qs: PROCESS_AFFINITY_UPDATE_MODE
            ProcessMemoryAllocationMode, // qs: PROCESS_MEMORY_ALLOCATION_MODE
            ProcessGroupInformation, // q: USHORT[]
            ProcessTokenVirtualizationEnabled, // s: ULONG
            ProcessConsoleHostProcess, // q: ULONG_PTR
            ProcessWindowInformation, // 50, q: PROCESS_WINDOW_INFORMATION
            ProcessHandleInformation, // q: PROCESS_HANDLE_SNAPSHOT_INFORMATION // since WIN8
            ProcessMitigationPolicy, // s: PROCESS_MITIGATION_POLICY_INFORMATION
            ProcessDynamicFunctionTableInformation,
            ProcessHandleCheckingMode,
            ProcessKeepAliveCount, // q: PROCESS_KEEPALIVE_COUNT_INFORMATION
            ProcessRevokeFileHandles, // s: PROCESS_REVOKE_FILE_HANDLES_INFORMATION
            MaxProcessInfoClass
        };
        #endregion

        #region const
        public const UInt32 PAGE_NOACCESS = 0x01;
        public const UInt32 PAGE_READONLY = 0x02;
        public const UInt32 PAGE_READWRITE = 0x04;
        public const UInt32 PAGE_WRITECOPY = 0x08;
        public const UInt32 PAGE_EXECUTE = 0x10;
        public const UInt32 PAGE_EXECUTE_READ = 0x20;
        public const UInt32 PAGE_EXECUTE_READWRITE = 0x40;
        public const UInt32 PAGE_EXECUTE_WRITECOPY = 0x80;
        public const UInt32 PAGE_GUARD = 0x100;
        public const UInt32 PAGE_NOCACHE = 0x200;
        public const UInt32 PAGE_WRITECOMBINE = 0x400;
        public const UInt32 PAGE_TARGETS_INVALID = 0x40000000;
        public const UInt32 PAGE_TARGETS_NO_UPDATE = 0x40000000;
        #endregion
        #endregion

        #region exports
        #region uncategorized dinvoke
        public static IntPtr NtAllocateVirtualMemoryD(IntPtr ProcessHandle, ref IntPtr BaseAddress, IntPtr ZeroBits, ref IntPtr RegionSize, AllocationType AllocationType, UInt32 Protect)
        {
            // Craft an array for the arguments
            object[] funcargs =
            {
                ProcessHandle, BaseAddress, ZeroBits, RegionSize, AllocationType, Protect
            };

            // Note here for myself: TODO
            // We have to std d/invoke some functions because to manual map, we will need these anyways, but we *should* be able to manual map ntdll first, then completely remove these limitations from there.
            NTSTATUS retValue = (NTSTATUS)Evasion.DInvoke.DynamicAPIInvoke(@"ntdll.dll", @"NtAllocateVirtualMemory", typeof(DELEGATES.NtAllocateVirtualMemory), ref funcargs);
            if (retValue == NTSTATUS.AccessDenied)
            {
                // STATUS_ACCESS_DENIED
                throw new UnauthorizedAccessException(DSTR(DSTR_ACCESS_DENIED));
            }
            if (retValue == NTSTATUS.AlreadyCommitted)
            {
                // STATUS_ALREADY_COMMITTED
                throw new Exception(DSTR(DSTR_ALREADY_COMMITTED));
            }
            if (retValue == NTSTATUS.CommitmentLimit)
            {
                // STATUS_COMMITMENT_LIMIT
                throw new Exception(DSTR(DSTR_LOW_ON_VMEM));
            }
            if (retValue == NTSTATUS.ConflictingAddresses)
            {
                // STATUS_CONFLICTING_ADDRESSES
                throw new Exception(DSTR(DSTR_CONFLICTING_ADDRESS));
            }
            if (retValue == NTSTATUS.InsufficientResources)
            {
                // STATUS_INSUFFICIENT_RESOURCES
                throw new Exception(DSTR(DSTR_INSUFFICIENT_RESOURCES));
            }
            if (retValue == NTSTATUS.InvalidHandle)
            {
                // STATUS_INVALID_HANDLE
                throw new Exception(DSTR(DSTR_INVALID_HANDLE));
            }
            if (retValue == NTSTATUS.InvalidPageProtection)
            {
                // STATUS_INVALID_PAGE_PROTECTION
                throw new Exception(DSTR(DSTR_INVALID_PAGE_PROTECT));
            }
            if (retValue == NTSTATUS.NoMemory)
            {
                // STATUS_NO_MEMORY
                throw new Exception(DSTR(DSTR_INSUFFICIENT_RESOURCES));
            }
            if (retValue == NTSTATUS.ObjectTypeMismatch)
            {
                // STATUS_OBJECT_TYPE_MISMATCH
                throw new Exception(DSTR(DSTR_OBJECT_TYPE_MISMATCH));
            }
            if (retValue != NTSTATUS.Success)
            {
                // STATUS_PROCESS_IS_TERMINATING == 0xC000010A
                throw new Exception(DSTR(DSTR_PROC_EXITING));
            }

            BaseAddress = (IntPtr)funcargs[1];
            return BaseAddress;
        }

        public static void RtlInitUnicodeStringD(ref UNICODE_STRING DestinationString, [MarshalAs(UnmanagedType.LPWStr)] string SourceString)
        {
            // Craft an array for the arguments
            object[] funcargs =
            {
                DestinationString, SourceString
            };

            Evasion.DInvoke.DynamicAPIInvoke(@"ntdll.dll", @"RtlInitUnicodeString", typeof(DELEGATES.RtlInitUnicodeString), ref funcargs);

            // Update the modified variables
            DestinationString = (UNICODE_STRING)funcargs[0];
        }

        public static NTSTATUS LdrLoadDllD(IntPtr PathToFile, UInt32 dwFlags, ref UNICODE_STRING ModuleFileName, ref IntPtr ModuleHandle)
        {
            // Craft an array for the arguments
            object[] funcargs =
            {
                PathToFile, dwFlags, ModuleFileName, ModuleHandle
            };

            NTSTATUS retValue = (NTSTATUS)Evasion.DInvoke.DynamicAPIInvoke(@"ntdll.dll", @"LdrLoadDll", typeof(DELEGATES.LdrLoadDll), ref funcargs);

            // Update the modified variables
            ModuleHandle = (IntPtr)funcargs[3];

            return retValue;
        }

        public static UInt32 NtWriteVirtualMemoryD(IntPtr ProcessHandle, IntPtr BaseAddress, IntPtr Buffer, UInt32 BufferLength)
        {
            // Craft an array for the arguments
            UInt32 BytesWritten = 0;
            object[] funcargs =
            {
                ProcessHandle, BaseAddress, Buffer, BufferLength, BytesWritten
            };

            NTSTATUS retValue = (NTSTATUS)Evasion.DInvoke.DynamicAPIInvoke(@"ntdll.dll", @"NtWriteVirtualMemory", typeof(DELEGATES.NtWriteVirtualMemory), ref funcargs);
            if (retValue != NTSTATUS.Success)
            {
                throw new Exception(DSTR(DSTR_FAILED_MEMORY_WRITE));
            }

            BytesWritten = (UInt32)funcargs[4];
            return BytesWritten;
        }

        public static void RtlGetVersionD(ref OSVERSIONINFOEX VersionInformation)
        {
            // Craft an array for the arguments
            object[] funcargs =
            {
                VersionInformation
            };

            NTSTATUS retValue = (NTSTATUS)Evasion.DInvoke.DynamicAPIInvoke(@"ntdll.dll", @"RtlGetVersion", typeof(DELEGATES.RtlGetVersion), ref funcargs);
            if (retValue != NTSTATUS.Success)
            {
                throw new Exception(DSTR(DSTR_PROC_ADDRESS_LOOKUP_FAILED, retValue));
            }

            VersionInformation = (OSVERSIONINFOEX)funcargs[0];
        }

        public static PROCESS_BASIC_INFORMATION NtQueryInformationProcessBasicInformationD(IntPtr hProcess)
        {
            NTSTATUS retValue = NtQueryInformationProcessD(hProcess, PROCESSINFOCLASS.ProcessBasicInformation, out IntPtr pProcInfo);
            if (retValue != NTSTATUS.Success)
            {
                throw new Exception(DSTR(DSTR_ACCESS_DENIED));
            }

            return (PROCESS_BASIC_INFORMATION)Marshal.PtrToStructure(pProcInfo, typeof(PROCESS_BASIC_INFORMATION));
        }

        public static NTSTATUS NtQueryInformationProcessD(IntPtr hProcess, PROCESSINFOCLASS processInfoClass, out IntPtr pProcInfo)
        {
            int processInformationLength;
            UInt32 RetLen = 0;

            switch (processInfoClass)
            {
                case PROCESSINFOCLASS.ProcessWow64Information:
                    pProcInfo = Marshal.AllocHGlobal(IntPtr.Size);
                    RtlZeroMemoryD(pProcInfo, IntPtr.Size);
                    processInformationLength = IntPtr.Size;
                    break;
                case PROCESSINFOCLASS.ProcessBasicInformation:
                    PROCESS_BASIC_INFORMATION PBI = new PROCESS_BASIC_INFORMATION();
                    pProcInfo = Marshal.AllocHGlobal(Marshal.SizeOf(PBI));
                    RtlZeroMemoryD(pProcInfo, Marshal.SizeOf(PBI));
                    Marshal.StructureToPtr(PBI, pProcInfo, true);
                    processInformationLength = Marshal.SizeOf(PBI);
                    break;
                default:
                    throw new Exception(DSTR(DSTR_INVALID_PROCINFOCLASS, processInfoClass));
            }

            object[] funcargs =
            {
                hProcess, processInfoClass, pProcInfo, processInformationLength, RetLen
            };

            NTSTATUS retValue = (NTSTATUS)Evasion.DInvoke.DynamicAPIInvoke(@"ntdll.dll", @"NtQueryInformationProcess", typeof(DELEGATES.NtQueryInformationProcess), ref funcargs);
            if (retValue != NTSTATUS.Success)
            {
                throw new UnauthorizedAccessException(DSTR(DSTR_ACCESS_DENIED));
            }

            // Update the modified variables
            pProcInfo = (IntPtr)funcargs[2];

            return retValue;
        }

        public static void RtlZeroMemoryD(IntPtr Destination, int Length)
        {
            // Craft an array for the arguments
            object[] funcargs =
            {
                Destination, Length
            };

            Evasion.DInvoke.DynamicAPIInvoke(@"ntdll.dll", @"RtlZeroMemory", typeof(DELEGATES.RtlZeroMemory), ref funcargs);
        }

        public static IntPtr LdrGetProcedureAddressD(IntPtr hModule, IntPtr FunctionName, IntPtr Ordinal, ref IntPtr FunctionAddress)
        {
            // Craft an array for the arguments
            object[] funcargs =
            {
                hModule, FunctionName, Ordinal, FunctionAddress
            };

            NTSTATUS retValue = (NTSTATUS)Evasion.DInvoke.DynamicAPIInvoke(@"ntdll.dll", @"LdrGetProcedureAddress", typeof(DELEGATES.LdrGetProcedureAddress), ref funcargs);
            if (retValue != NTSTATUS.Success)
            {
                throw new Exception(DSTR(DSTR_PROC_ADDRESS_LOOKUP_FAILED, retValue));
            }

            FunctionAddress = (IntPtr)funcargs[3];
            return FunctionAddress;
        }

        public static UInt32 NtProtectVirtualMemoryD(IntPtr ProcessHandle, ref IntPtr BaseAddress, ref IntPtr RegionSize, UInt32 NewProtect)
        {
            // Craft an array for the arguments
            UInt32 OldProtect = 0;
            object[] funcargs =
            {
                ProcessHandle, BaseAddress, RegionSize, NewProtect, OldProtect
            };

            NTSTATUS retValue = (NTSTATUS)Evasion.DInvoke.DynamicAPIInvoke(@"ntdll.dll", @"NtProtectVirtualMemory", typeof(DELEGATES.NtProtectVirtualMemory), ref funcargs);
            if (retValue != NTSTATUS.Success)
            {
                throw new Exception(DSTR(DSTR_FAILED_MEMPROTECT, retValue));
            }

            OldProtect = (UInt32)funcargs[4];
            return OldProtect;
        }
        #endregion

        #region categorized P = PInvoke, D = DInvoke, M = Manual DInvoke
        #region process

        #region WriteProcessMemory
        #if USE_PINVOKE
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(PointerEx hProcess, PointerEx lpBaseAddress, byte[] lpBuffer, int dwSize, ref PointerEx lpNumberOfBytesWritten);

        public static bool WriteProcessMemoryP(PointerEx hProcess, PointerEx lpBaseAddress, byte[] lpBuffer, int dwSize, ref PointerEx lpNumberOfBytesWritten)
        {
            return WriteProcessMemory(hProcess, lpBaseAddress, lpBuffer, dwSize, ref lpNumberOfBytesWritten);
        }
        #else
        public static bool WriteProcessMemoryD(PointerEx hProcess, PointerEx lpBaseAddress, byte[] lpBuffer, int dwSize, ref PointerEx lpNumberOfBytesWritten)
        {
            // Craft an array for the arguments
            object[] funcargs =
            {
                hProcess, lpBaseAddress, lpBuffer, dwSize, lpNumberOfBytesWritten
            };

            bool retValue = (bool)Evasion.DInvoke.DynamicAPIInvoke(CONST_KERNEL32, @"WriteProcessMemory", typeof(_WriteProcessMemory), ref funcargs);
            lpNumberOfBytesWritten = (PointerEx)funcargs[funcargs.Length - 1];

            return retValue;
        }
        #endif
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate bool _WriteProcessMemory(PointerEx hProcess, PointerEx lpBaseAddress, byte[] lpBuffer, int dwSize, ref PointerEx lpNumberOfBytesWritten);
        public static bool WriteProcessMemoryM(PointerEx hProcess, PointerEx lpBaseAddress, byte[] lpBuffer, int dwSize, ref PointerEx lpNumberOfBytesWritten)
        {
            // Craft an array for the arguments
            object[] funcargs =
            {
                hProcess, lpBaseAddress, lpBuffer, dwSize, lpNumberOfBytesWritten
            };

            bool retValue = (bool)Evasion.DInvoke.ManualInvoke(CONST_KERNEL32, @"WriteProcessMemory", typeof(_WriteProcessMemory), ref funcargs);
            lpNumberOfBytesWritten = (PointerEx)funcargs[funcargs.Length - 1];

            return retValue;
        }
        #endregion

        #region ReadProcessMemory
        #if USE_PINVOKE
        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(PointerEx hProcess, PointerEx lpBaseAddress, [Out] byte[] lpBuffer, PointerEx dwSize, ref PointerEx lpNumberOfBytesRead);
        public static bool ReadProcessMemoryP(PointerEx hProcess, PointerEx lpBaseAddress, byte[] lpBuffer, PointerEx dwSize, ref PointerEx lpNumberOfBytesRead)
        {
            return ReadProcessMemory(hProcess, lpBaseAddress, lpBuffer, dwSize, ref lpNumberOfBytesRead);
        }
        #else
        public static bool ReadProcessMemoryD(PointerEx hProcess, PointerEx lpBaseAddress, byte[] lpBuffer, PointerEx dwSize, ref PointerEx lpNumberOfBytesRead)
        {
            object[] funcArgs =
            {
                hProcess, lpBaseAddress, lpBuffer, dwSize, lpNumberOfBytesRead
            };

            bool retValue = (bool)Evasion.DInvoke.DynamicAPIInvoke(CONST_KERNEL32, @"ReadProcessMemory", typeof(_ReadProcessMemory), ref funcArgs);
            lpNumberOfBytesRead = (PointerEx)funcArgs[funcArgs.Length - 1];
            if(lpNumberOfBytesRead <= lpBuffer.Length)
            {
                Array.Copy((byte[])funcArgs[2], lpBuffer, lpNumberOfBytesRead);
            }
            else
            {
                throw new Exception(DSTR(DSTR_BUFFER_TOO_SMALL));
            }
            return retValue;
        }
        #endif
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate bool _ReadProcessMemory(PointerEx hProcess, PointerEx lpBaseAddress, byte[] lpBuffer, PointerEx dwSize, ref PointerEx lpNumberOfBytesRead);

        public static bool ReadProcessMemoryM(PointerEx hProcess, PointerEx lpBaseAddress, byte[] lpBuffer, PointerEx dwSize, ref PointerEx lpNumberOfBytesRead)
        {
            object[] funcArgs =
            {
                hProcess, lpBaseAddress, lpBuffer, dwSize, lpNumberOfBytesRead
            };

            bool retValue = (bool)Evasion.DInvoke.ManualInvoke(CONST_KERNEL32, @"ReadProcessMemory", typeof(_ReadProcessMemory), ref funcArgs);
            lpNumberOfBytesRead = (PointerEx)funcArgs[funcArgs.Length - 1];
            if (lpNumberOfBytesRead <= lpBuffer.Length)
            {
                Array.Copy((byte[])funcArgs[2], lpBuffer, lpNumberOfBytesRead);
            }
            else
            {
                throw new Exception(DSTR(DSTR_BUFFER_TOO_SMALL));
            }
            return retValue;
        }
        #endregion

        #region VirtualAllocEx
        #if USE_PINVOKE
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern PointerEx VirtualAllocEx(PointerEx hProcess, PointerEx lpAddress, uint dwSize, AllocationType flAllocationType, MemoryProtection flProtect);

        public static PointerEx VirtualAllocExP(PointerEx hProcess, PointerEx lpAddress, uint dwSize, AllocationType flAllocationType, MemoryProtection flProtect)
        {
            return VirtualAllocEx(hProcess, lpAddress, dwSize, flAllocationType, flProtect);
        }
        #else
        public static PointerEx VirtualAllocExD(PointerEx hProcess, PointerEx lpAddress, uint dwSize, AllocationType flAllocationType, MemoryProtection flProtect)
        {
            object[] funcArgs =
            {
                hProcess, lpAddress, dwSize, flAllocationType, flProtect
            };
            return (PointerEx)Evasion.DInvoke.DynamicAPIInvoke(CONST_KERNEL32, @"VirtualAllocEx", typeof(_VirtualAllocEx), ref funcArgs);
        }
        #endif
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate PointerEx _VirtualAllocEx(PointerEx hProcess, PointerEx lpAddress, uint dwSize, AllocationType flAllocationType, MemoryProtection flProtect);
        public static PointerEx VirtualAllocExM(PointerEx hProcess, PointerEx lpAddress, uint dwSize, AllocationType flAllocationType, MemoryProtection flProtect)
        {
            object[] funcArgs =
            {
                hProcess, lpAddress, dwSize, flAllocationType, flProtect
            };
            return (PointerEx)Evasion.DInvoke.ManualInvoke(CONST_KERNEL32, @"VirtualAllocEx", typeof(_VirtualAllocEx), ref funcArgs);
        }
        #endregion

        #region VirtualProtectEx
#if USE_PINVOKE
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualProtectEx(IntPtr processHandle, IntPtr address, int size, int protectionType, out int oldProtectionType);
        public static bool VirtualProtectExP(IntPtr processHandle, IntPtr address, int size, int protectionType, out int oldProtectionType)
        {
            return VirtualProtectEx(processHandle, address, size, protectionType, out oldProtectionType);
        }
#else
        public static bool VirtualProtectExD(IntPtr processHandle, IntPtr address, int size, int protectionType, out int oldProtectionType)
        {
            oldProtectionType = 0;
            object[] funcArgs =
            {
                processHandle, address, size, protectionType, oldProtectionType
            };
            bool result = (bool)Evasion.DInvoke.DynamicAPIInvoke(CONST_KERNEL32, "VirtualProtectEx", typeof(_VirtualProtectEx), ref funcArgs);
            oldProtectionType = (int)funcArgs[funcArgs.Length - 1];
            return result;
        }
#endif
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate bool _VirtualProtectEx(IntPtr processHandle, IntPtr address, int size, int protectionType, out int oldProtectionType);
        public static bool VirtualProtectExM(IntPtr processHandle, IntPtr address, int size, int protectionType, out int oldProtectionType)
        {
            oldProtectionType = 0;
            object[] funcArgs =
            {
                processHandle, address, size, protectionType, oldProtectionType
            };
            bool result = (bool)Evasion.DInvoke.ManualInvoke(CONST_KERNEL32, "VirtualProtectEx", typeof(_VirtualProtectEx), ref funcArgs);
            oldProtectionType = (int)funcArgs[funcArgs.Length - 1];
            return result;
        }
        #endregion

        #region LoadLibrary
        #if USE_PINVOKE
        [DllImport("kernel32", SetLastError=true, CharSet = CharSet.Ansi)]
        private static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)]string lpFileName);

        public static IntPtr LoadLibraryP(string lpFileName)
        {
            return LoadLibrary(lpFileName);
        }
        #else
        public static IntPtr LoadLibraryD(string lpFileName)
        {
            // Craft an array for the arguments
            object[] funcargs =
            {
                lpFileName
            };
            return (IntPtr)Evasion.DInvoke.DynamicAPIInvoke(CONST_KERNEL32, @"LoadLibrary", typeof(_LoadLibrary), ref funcargs);
        }
        #endif
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate IntPtr _LoadLibrary(string lpFileName);
        public static IntPtr LoadLibraryM(string lpFileName)
        {
            // Craft an array for the arguments
            object[] funcargs =
            {
                lpFileName
            };
            return (IntPtr)Evasion.DInvoke.ManualInvoke(CONST_KERNEL32, @"LoadLibrary", typeof(_LoadLibrary), ref funcargs); ;
        }
        #endregion

        #endregion
        #region thread

        #region OpenThread
#if USE_PINVOKE
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern PointerEx OpenThread(int dwDesiredAccess, bool bInheritHandle, int dwThreadId);
        public static PointerEx OpenThreadP(int dwDesiredAccess, bool bInheritHandle, int dwThreadId)
        {
            return OpenThread(dwDesiredAccess, bInheritHandle, dwThreadId);
        }
#else
        public static PointerEx OpenThreadD(int dwDesiredAccess, bool bInheritHandle, int dwThreadId)
        {
            object[] funcArgs =
            {
                dwDesiredAccess, bInheritHandle, dwThreadId
            };

            return (PointerEx)Evasion.DInvoke.DynamicAPIInvoke(CONST_KERNEL32, "OpenThread", typeof(_OpenThread), ref funcArgs);
        }
#endif
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate PointerEx _OpenThread(int dwDesiredAccess, bool bInheritHandle, int dwThreadId);
        public static PointerEx OpenThreadM(int dwDesiredAccess, bool bInheritHandle, int dwThreadId)
        {
            object[] funcArgs =
            {
                dwDesiredAccess, bInheritHandle, dwThreadId
            };

            return (PointerEx)Evasion.DInvoke.ManualInvoke(CONST_KERNEL32, "OpenThread", typeof(_OpenThread), ref funcArgs);
        }
#endregion

#region SuspendThread
#if USE_PINVOKE
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern PointerEx SuspendThread(PointerEx hThread);
        public static PointerEx SuspendThreadP(PointerEx hThread)
        {
            return SuspendThread(hThread);
        }
#else
        public static PointerEx SuspendThreadD(PointerEx hThread)
        {
            object[] funcArgs =
            {
                hThread
            };
            return (PointerEx)Evasion.DInvoke.DynamicAPIInvoke(CONST_KERNEL32, "SuspendThread", typeof(_SuspendThread), ref funcArgs);
        }
#endif
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate PointerEx _SuspendThread(PointerEx hThread);
        public static PointerEx SuspendThreadM(PointerEx hThread)
        {
            object[] funcArgs =
            {
                hThread
            };
            return (PointerEx)Evasion.DInvoke.ManualInvoke(CONST_KERNEL32, "SuspendThread", typeof(_SuspendThread), ref funcArgs);
        }
#endregion

#region GetThreadContext
#if USE_PINVOKE
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetThreadContext(IntPtr hThread, IntPtr lpContext);
        public static bool GetThreadContextP(IntPtr hThread, IntPtr lpContext)
        {
            return GetThreadContext(hThread, lpContext);
        }
#else
        public static bool GetThreadContextD(IntPtr hThread, IntPtr lpContext)
        {
            object[] funcArgs =
            {
                hThread, lpContext
            };
            return (bool)Evasion.DInvoke.DynamicAPIInvoke(CONST_KERNEL32, "GetThreadContext", typeof(_GetThreadContext), ref funcArgs);
        }
#endif
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate bool _GetThreadContext(IntPtr hThread, IntPtr lpContext);
        public static bool GetThreadContextM(IntPtr hThread, IntPtr lpContext)
        {
            object[] funcArgs =
            {
                hThread, lpContext
            };
            return (bool)Evasion.DInvoke.ManualInvoke(CONST_KERNEL32, "GetThreadContext", typeof(_GetThreadContext), ref funcArgs);
        }
#endregion

#region SetThreadContext
#if USE_PINVOKE
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetThreadContext(IntPtr hThread, IntPtr lpContext);
        public static bool SetThreadContextP(IntPtr hThread, IntPtr lpContext)
        {
            return SetThreadContext(hThread, lpContext);
        }
#else
        public static bool SetThreadContextD(IntPtr hThread, IntPtr lpContext)
        {
            object[] funcArgs =
            {
                hThread, lpContext
            };
            return (bool)Evasion.DInvoke.DynamicAPIInvoke(CONST_KERNEL32, "SetThreadContext", typeof(_SetThreadContext), ref funcArgs);
        }
#endif
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate bool _SetThreadContext(IntPtr hThread, IntPtr lpContext);
        public static bool SetThreadContextM(IntPtr hThread, IntPtr lpContext)
        {
            object[] funcArgs =
            {
                hThread, lpContext
            };
            return (bool)Evasion.DInvoke.ManualInvoke(CONST_KERNEL32, "SetThreadContext", typeof(_SetThreadContext), ref funcArgs);
        }
#endregion

#region ResumeThread
#if USE_PINVOKE
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint ResumeThread(PointerEx hThread);
        public static uint ResumeThreadP(PointerEx hThread)
        {
            return ResumeThread(hThread);
        }
#else
        public static uint ResumeThreadD(PointerEx hThread)
        {
            object[] funcArgs = new object[] { hThread };
            return (uint)Evasion.DInvoke.DynamicAPIInvoke(CONST_KERNEL32, "ResumeThread", typeof(_ResumeThread), ref funcArgs);
        }
#endif
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate uint _ResumeThread(PointerEx hThread);
        public static uint ResumeThreadM(PointerEx hThread)
        {
            object[] funcArgs = new object[] { hThread };
            return (uint)Evasion.DInvoke.ManualInvoke(CONST_KERNEL32, "ResumeThread", typeof(_ResumeThread), ref funcArgs);
        }
        #endregion

#region QueueUserAPC2
#if USE_PINVOKE
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint QueueUserAPC2(PointerEx pfnAPC, PointerEx hThread, PointerEx dwData, int flags);
        public static uint QueueUserAPC2P(PointerEx pfnAPC, PointerEx hThread, PointerEx dwData, int flags)
        {
            throw new NotImplementedException();
        }
#else
        public static uint QueueUserAPC2D(PointerEx pfnAPC, PointerEx hThread, int flags)
        {
            object[] funcArgs = new object[] { (ulong)hThread, (ulong)flags, (ulong)pfnAPC, (ulong)0, (ulong)0, (ulong)0 };
            return (uint)Evasion.DInvoke.DynamicAPIInvoke(CONST_NTDLL, "NtQueueApcThreadEx", typeof(_NtQueueApcThreadEx), ref funcArgs);
        }
#endif
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate uint _NtQueueApcThreadEx(ulong hThread, ulong flags, ulong pfnAPC, ulong a4, ulong a5, ulong a6);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate uint _QueueUserAPC2(PointerEx pfnAPC, PointerEx hThread, int flags);
        public static uint QueueUserAPC2M(PointerEx pfnAPC, PointerEx hThread, int flags)
        {
            object[] funcArgs = new object[] { (ulong)hThread, (ulong)flags, (ulong)pfnAPC, (ulong)0, (ulong)0, (ulong)0 };
            return (uint)Evasion.DInvoke.ManualInvoke(CONST_NTDLL, "NtQueueApcThreadEx", typeof(_NtQueueApcThreadEx), ref funcArgs);
        }
        #endregion

        #endregion
        #endregion
        #endregion

        #region DInvoke Internal Registry
        private struct DELEGATES
        {
            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate UInt32 NtAllocateVirtualMemory(
                IntPtr ProcessHandle,
                ref IntPtr BaseAddress,
                IntPtr ZeroBits,
                ref IntPtr RegionSize,
                AllocationType AllocationType,
                UInt32 Protect);

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate void RtlInitUnicodeString(
                ref UNICODE_STRING DestinationString,
                [MarshalAs(UnmanagedType.LPWStr)]
                string SourceString);

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate UInt32 LdrLoadDll(
                IntPtr PathToFile,
                UInt32 dwFlags,
                ref UNICODE_STRING ModuleFileName,
                ref IntPtr ModuleHandle);

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate UInt32 NtWriteVirtualMemory(
                IntPtr ProcessHandle,
                IntPtr BaseAddress,
                IntPtr Buffer,
                UInt32 BufferLength,
                ref UInt32 BytesWritten);

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate UInt32 RtlGetVersion(
                ref OSVERSIONINFOEX VersionInformation);

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate UInt32 NtQueryInformationProcess(
                IntPtr processHandle,
               PROCESSINFOCLASS processInformationClass,
                IntPtr processInformation,
                int processInformationLength,
                ref UInt32 returnLength);

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate void RtlZeroMemory(
                IntPtr Destination,
                int length);

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate UInt32 LdrGetProcedureAddress(
                IntPtr hModule,
                IntPtr FunctionName,
                IntPtr Ordinal,
                ref IntPtr FunctionAddress);

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate UInt32 NtProtectVirtualMemory(
                IntPtr ProcessHandle,
                ref IntPtr BaseAddress,
                ref IntPtr RegionSize,
                UInt32 NewProtect,
                ref UInt32 OldProtect);
        }
#endregion
    }

    public static class NativeStealth
    {
        public static NativeStealthType Stealth { get; private set; }
        public static Native._WriteProcessMemory WriteProcessMemory { get; private set; }
        public static Native._ReadProcessMemory ReadProcessMemory { get; private set; }
        public static Native._VirtualAllocEx VirtualAllocEx { get; private set; }
        public static Native._VirtualProtectEx VirtualProtectEx { get; private set; }
        public static Native._OpenThread OpenThread { get; private set; }
        public static Native._SuspendThread SuspendThread { get; private set; }
        public static Native._GetThreadContext GetThreadContext { get; private set; }
        public static Native._SetThreadContext SetThreadContext { get; private set; }
        public static Native._ResumeThread ResumeThread { get; private set; }
        public static Native._LoadLibrary LoadLibrary { get; private set; }
        public static Native._QueueUserAPC2 QueueUserAPC2 { get; private set; }

        private static void StealthUpdate()
        {
            if(Stealth == NativeStealthType.ManualInvoke)
            {
                WriteProcessMemory = Native.WriteProcessMemoryM;
                ReadProcessMemory = Native.ReadProcessMemoryM;
                VirtualAllocEx = Native.VirtualAllocExM;
                VirtualProtectEx = Native.VirtualProtectExM;
                OpenThread = Native.OpenThreadM;
                SuspendThread = Native.SuspendThreadM;
                GetThreadContext = Native.GetThreadContextM;
                SetThreadContext = Native.SetThreadContextM;
                ResumeThread = Native.ResumeThreadM;
                LoadLibrary = Native.LoadLibraryM;
                QueueUserAPC2 = Native.QueueUserAPC2M;
            }
            else
            {
#if USE_PINVOKE
                WriteProcessMemory = Native.WriteProcessMemoryP;
                ReadProcessMemory = Native.ReadProcessMemoryP;
                VirtualAllocEx = Native.VirtualAllocExP;
                VirtualProtectEx = Native.VirtualProtectExP;
                OpenThread = Native.OpenThreadP;
                SuspendThread = Native.SuspendThreadP;
                GetThreadContext = Native.GetThreadContextP;
                SetThreadContext = Native.SetThreadContextP;
                ResumeThread = Native.ResumeThreadP;
                LoadLibrary = Native.LoadLibraryP;
                QueueUserAPC2 = Native.QueueUserAPC2P;
#else
                WriteProcessMemory = Native.WriteProcessMemoryD;
                ReadProcessMemory = Native.ReadProcessMemoryD;
                VirtualAllocEx = Native.VirtualAllocExD;
                VirtualProtectEx = Native.VirtualProtectExD;
                OpenThread = Native.OpenThreadD;
                SuspendThread = Native.SuspendThreadD;
                GetThreadContext = Native.GetThreadContextD;
                SetThreadContext = Native.SetThreadContextD;
                ResumeThread = Native.ResumeThreadD;
                LoadLibrary = Native.LoadLibraryD;
                QueueUserAPC2 = Native.QueueUserAPC2D;
#endif
            }
        }

        static NativeStealth()
        {
            StealthUpdate();
        }

        public static void SetStealthMode(NativeStealthType type)
        {
            Stealth = type;
            StealthUpdate();
        }
    }

    public enum NativeStealthType
    {
#if USE_PINVOKE
        /// <summary>
        /// Use standard pinvoke calls to dlls
        /// </summary>
        PInvoke,
#else
        /// <summary>
        /// Use dynamic invocation of calls, to prevent api hooking. Only useful if USE_PINVOKE is not defined in symbols for external at build time
        /// </summary>
        DInvoke,
#endif
        /// <summary>
        /// Use manual mapped instances of all dlls for function invocation. May be used regardless of USE_PINVOKE.
        /// </summary>
        ManualInvoke
    }
}