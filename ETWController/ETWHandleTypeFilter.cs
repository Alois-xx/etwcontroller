using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;


namespace ETWController
{

    /// <summary>
    /// Enable for handle leak tracing to filter during recording for specific handle types to limit the amount of traced
    /// data to enable long(er) tracing sessions to make it easier to capture the actual leak. 
    /// </summary>
    internal class ETWHandleTypeFilter
    {
        static Dictionary<string, uint> ObjectTypeToId = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase)
        {
            { "Disable", 0 },
            { "ALPC Port", CreateId("ALPC Port") },
            { "Composition", CreateId("Composition") },
            { "CoreMessaging", CreateId("CoreMessaging") },
            { "Desktop", CreateId("Desktop") },
            { "Device", CreateId("Device") },
            { "Directory", CreateId("Directory") },
            { "DxgkCompositionObject", CreateId("DxgkCompositionObject") },
            { "DxgkDisplayManagerObject", CreateId("DxgkDisplayManagerObject") },
            { "DxgkSharedResource", CreateId("DxgkSharedResource") },
            { "DxgkSharedSyncObject", CreateId("DxgkSharedSyncObject") },
            { "EnergyTracker", CreateId("EnergyTracker") },
            { "EtwConsumer", CreateId("EtwConsumer") },
            { "EtwRegistration", CreateId("EtwRegistration") },
            { "Event", CreateId("Event") },
            { "File", CreateId("File") },
            { "FilterCommunicationPort", CreateId("FilterCommunicationPort") },
            { "FilterConnectionPort", CreateId("FilterConnectionPort") },
            { "IRTimer", CreateId("IRTimer") },
            { "IoCompletion", CreateId("IoCompletion") },
            { "IoCompletionReserve", CreateId("IoCompletionReserve") },
            { "Job", CreateId("Job") },
            { "Key", CreateId("Key") },
            { "Mutant", CreateId("Mutant") },
            { "Partition", CreateId("Partition") },
            { "PcwObject", CreateId("PcwObject") },
            { "PowerRequest", CreateId("PowerRequest") },
            { "Process", CreateId("Process") },
            { "RawInputManager", CreateId("RawInputManager") },
            { "Section", CreateId("Section") },
            { "Semaphore", CreateId("Semaphore") },
            { "Session", CreateId("Session") },
            { "SymbolicLink", CreateId("SymbolicLink") },
            { "Thread", CreateId("Thread") },
            { "Timer", CreateId("Timer") },
            { "TmEn", CreateId("TmEn") },
            { "TmRm", CreateId("TmRm") },
            { "TmTm", CreateId("TmTm") },
            { "TmTx", CreateId("TmTx") },
            { "Token", CreateId("Token") },
            { "TpWorkerFactory", CreateId("TpWorkerFactory") },
            { "UserApcReserve", CreateId("UserApcReserve") },
            { "WaitCompletionPacket", CreateId("WaitCompletionPacket") },
            { "WindowStation", CreateId("WindowStation") },
            { "WmiGuid", CreateId("WmiGuid") },
        };

        /// <summary>
        /// Object trace name is an integer with the first 4 characters of the object type name. If the name is shorter than 4 chars
        /// the remaining characters are filled up with spaces. 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        static UInt32 CreateId(string name)
        {
            byte[] chars = name.ToCharArray().Append(' ').Append(' ').Append(' ').Take(4).Select(c => (byte)c).ToArray();
            int filter = Marshal.ReadInt32(chars, 0);
            return (UInt32)filter;
        }

        /// <summary>
        /// Main method to set undocumented Windows properties.
        /// </summary>
        /// <param name="InfoClass"></param>
        /// <param name="Info"></param>
        /// <param name="Length"></param>
        /// <returns>0 on success as NTStatus or a different value on error.</returns>
        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern UInt32 NtSetSystemInformation(uint InfoClass, ref EVENT_TRACE_TAG_FILTER_INFORMATION Info, int Length);

        /// <summary>
        /// Update/set the object type filter when handle tracing is enabled.
        /// </summary>
        const uint EventTraceObjectTypeFilterInformation = 0x11;

        /// <summary>
        /// This value is needed by NtSetSystemInformation to set ETW session properties.
        /// </summary>
        const uint SystemPerformanceTraceInformation = 0x1F;

        /// <summary>
        /// At most 4 filters can be set at once
        /// </summary>
        const int ETW_MAX_TAG_FILTER = 4;

        /// <summary>
        /// This are the supported values copied from SystemInformer which seems to have an up to date list 
        /// </summary>
        public enum EventTraceInformationClass : uint
        {
            KernelVersionInformation, // EVENT_TRACE_VERSION_INFORMATION
            GroupMaskInformation, // EVENT_TRACE_GROUPMASK_INFORMATION
            PerformanceInformation, // EVENT_TRACE_PERFORMANCE_INFORMATION
            TimeProfileInformation, // EVENT_TRACE_TIME_PROFILE_INFORMATION
            SessionSecurityInformation, // EVENT_TRACE_SESSION_SECURITY_INFORMATION
            SpinlockInformation, // EVENT_TRACE_SPINLOCK_INFORMATION
            StackTracingInformation, // EVENT_TRACE_STACK_TRACING_INFORMATION
            ExecutiveResourceInformation, // EVENT_TRACE_EXECUTIVE_RESOURCE_INFORMATION
            HeapTracingInformation, // EVENT_TRACE_HEAP_TRACING_INFORMATION
            HeapSummaryTracingInformation, // EVENT_TRACE_HEAP_TRACING_INFORMATION
            PoolTagFilterInformation, // EVENT_TRACE_POOLTAG_FILTER_INFORMATION
            PebsTracingInformation, // EVENT_TRACE_PEBS_TRACING_INFORMATION
            ProfileConfigInformation, // EVENT_TRACE_PROFILE_CONFIG_INFORMATION
            ProfileSourceListInformation, // EVENT_TRACE_PROFILE_LIST_INFORMATION
            ProfileEventListInformation, // EVENT_TRACE_PROFILE_EVENT_INFORMATION
            ProfileCounterListInformation, // EVENT_TRACE_PROFILE_COUNTER_INFORMATION
            StackCachingInformation, // EVENT_TRACE_STACK_CACHING_INFORMATION
            ObjectTypeFilterInformation, // EVENT_TRACE_OBJECT_TYPE_FILTER_INFORMATION
            SoftRestartInformation, // EVENT_TRACE_SOFT_RESTART_INFORMATION
            LastBranchConfigurationInformation, // REDSTONE3
            LastBranchEventListInformation,
            ProfileSourceAddInformation, // EVENT_TRACE_PROFILE_ADD_INFORMATION // REDSTONE4
            ProfileSourceRemoveInformation, // EVENT_TRACE_PROFILE_REMOVE_INFORMATION
            ProcessorTraceConfigurationInformation,
            ProcessorTraceEventListInformation,
            CoverageSamplerInformation, // EVENT_TRACE_COVERAGE_SAMPLER_INFORMATION
            UnifiedStackCachingInformation, // since 21H1
            MaxEventTraceInfoClass
        }


        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct EVENT_TRACE_TAG_FILTER_INFORMATION
        {
            public EventTraceInformationClass EventTraceInformationClass;
            public UInt64 TraceHandle;
            public fixed UInt32 Filters[ETW_MAX_TAG_FILTER]; // up to 4 filters can be set
        }

       
        /// <summary>
        /// Disable event filters
        /// </summary>
        public void Disable()
        {
            Enable(new List<string> { "Disable" });
        }

        /// <summary>
        /// Get Handle type filter names
        /// </summary>
        /// <returns></returns>
        public List<string> GetFilterNames()
        {
            return ObjectTypeToId.Where(x=>x.Key != "Disable").Select(x=>x.Key).ToList();   
        }

        /// <summary>
        /// Enable up to 4 handle type filters to allow much longer recording of handle leak tracing data.
        /// </summary>
        /// <param name="filterNames"></param>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public unsafe void Enable(List<string> filterNames)
        {
            if(filterNames.Count > ETW_MAX_TAG_FILTER )
            {
                throw new NotSupportedException($"You have entered {filterNames.Count} filters, but only up to {ETW_MAX_TAG_FILTER} are supported.");
            }

            List<uint> typeFilters = new List<uint>();
            foreach (var arg in filterNames)
            {
                if (!ObjectTypeToId.TryGetValue(arg, out uint typeFilter))
                {
                    throw new NotSupportedException($"Invalid handle filter type {arg}");
                }
                else
                {
                    typeFilters.Add(typeFilter);
                }
            }

            var kernel = TraceSession.GetActiveSessionNames();
            foreach (var kernelSession in kernel)
            {
                TraceSession session = new TraceSession(kernelSession);
                if (session.KernelFlags != 0)
                {
                    Debug.Print($"Found Kernel Session: {kernelSession} with handle 0x{session.SessionHandle:X}");

                    var tagFilter = new EVENT_TRACE_TAG_FILTER_INFORMATION();

                    tagFilter.TraceHandle = session.SessionHandle;
                    tagFilter.EventTraceInformationClass = EventTraceInformationClass.ObjectTypeFilterInformation;

                    for (int i = 0; i < typeFilters.Count; i++)
                    {
                        tagFilter.Filters[i] = typeFilters[i];
                    }

                    int size = Marshal.SizeOf<EVENT_TRACE_TAG_FILTER_INFORMATION>() - (ETW_MAX_TAG_FILTER - typeFilters.Count) * sizeof(UInt32);
                    uint lret = NtSetSystemInformation(SystemPerformanceTraceInformation,ref tagFilter, size);
                    if (lret != 0)
                    {
                        throw new InvalidOperationException($"NtSetSystemInformation: 0x{lret:X}, Size: {size} bytes");
                    }
                }
            }
        }
    }


    // Adapted from TraceEvent Library https://github.com/microsoft/perfview/tree/main/src/TraceEvent

#pragma warning disable 469, 649
    /// <summary>
    /// EventTraceHeader structure used by EVENT_TRACE_PROPERTIES
    /// </summary>
    internal struct WNODE_HEADER
    {
        public uint BufferSize;

        public uint ProviderId;

        public ulong HistoricalContext;

        public ulong TimeStamp;

        public Guid Guid;

        public uint ClientContext;

        public uint Flags;
    }

#pragma warning disable 469, 649
    /// <summary>
    /// EVENT_TRACE_PROPERTIES is a structure used by StartTrace, ControlTrace
    /// however it can not be used directly in the definition of these functions
    /// because extra information has to be hung off the end of the structure
    /// before being passed.  (LofFileNameOffset, LoggerNameOffset)
    /// </summary>
    internal struct EVENT_TRACE_PROPERTIES
    {
        public WNODE_HEADER Wnode;

        public uint BufferSize;

        public uint MinimumBuffers;

        public uint MaximumBuffers;

        public uint MaximumFileSize;

        public uint LogFileMode;

        public uint FlushTimer;

        public uint EnableFlags;

        public int AgeLimit;

        public uint NumberOfBuffers;

        public uint FreeBuffers;

        public uint EventsLost;

        public uint BuffersWritten;

        public uint LogBuffersLost;

        public uint RealTimeBuffersLost;

        public IntPtr LoggerThreadId;

        public uint LogFileNameOffset;

        public uint LoggerNameOffset;
    }


    unsafe class TraceSession
    {
        private static readonly int PropertiesSize = sizeof(EVENT_TRACE_PROPERTIES) + 4096 + 256;

        [SuppressUnmanagedCodeSecurity]
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern void ZeroMemory(IntPtr handle, int length);

        [SuppressUnmanagedCodeSecurity]
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        internal unsafe static extern int ControlTrace(ulong sessionHandle, string sessionName, EVENT_TRACE_PROPERTIES* properties, uint controlCode);

        [SuppressUnmanagedCodeSecurity]
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        internal static extern int QueryAllTraces([In] IntPtr propertyArray, [In] int propertyArrayCount, [In][Out] ref int sessionCount);


        private string m_FileName;

        private int m_BufferSizeMB;

        private int m_BufferQuantumKB;

        private int m_CircularBufferMB;

        private int m_MultiFileMB;

        public string SessionName
        {
            get;
            private set;
        }

        public ulong SessionHandle
        {
            get;
            private set;
        }

        public string FileName
        {
            get
            {
                return m_FileName;
            }

            set
            {
                m_FileName = value;
            }
        }

        public int BufferSizeMB
        {
            get
            {
                return m_BufferSizeMB;
            }

            set
            {
                m_BufferSizeMB = value;
            }
        }

        public int BufferQuantumKB
        {
            get
            {
                return m_BufferQuantumKB;
            }

            set
            {
                m_BufferQuantumKB = value;
            }
        }

        public int CircularBufferMB
        {
            get
            {
                return m_CircularBufferMB;
            }

            set
            {
                m_CircularBufferMB = value;
            }
        }

        public int MultiFileMB
        {
            get
            {
                return m_MultiFileMB;
            }

            set
            {
                m_MultiFileMB = value;
            }
        }

        public uint KernelFlags
        {
            get;
            private set;
        }

        public TraceSession(string sessionName)
        {
            this.SessionName = sessionName;
            this.m_BufferQuantumKB = 64;

            byte* buffer = stackalloc byte[PropertiesSize];
            EVENT_TRACE_PROPERTIES* properties = this.GetProperties(buffer);
            int num = ControlTrace(0uL, sessionName, properties, 0u);
            if ((long)num == 4201L)
            {
                throw new FileNotFoundException("The session " + sessionName + " is not active.");
            }
            this.SessionHandle = (ulong)properties->Wnode.HistoricalContext;
            this.KernelFlags = properties->EnableFlags;
            Marshal.ThrowExceptionForHR(GetHRFromWin32(num));
            if (properties->LogFileNameOffset != 0u)
            {
                this.m_FileName = new string((char*)(properties + properties->LogFileNameOffset / (uint)sizeof(EVENT_TRACE_PROPERTIES)));
                if (this.m_FileName.Length == 0)
                {
                    this.m_FileName = null;
                }
            }
            if (properties->BufferSize != 0u)
            {
                this.m_BufferQuantumKB = (int)properties->BufferSize;
            }
            this.m_BufferSizeMB = (int)((ulong)properties->MinimumBuffers * (ulong)((long)this.m_BufferQuantumKB)) / 1024;
            if ((properties->LogFileMode & 2u) != 0u)
            {
                this.m_CircularBufferMB = (int)properties->MaximumFileSize;
            }
            if ((properties->LogFileMode & 1024u) != 0u)
            {
                this.m_FileName = null;
                this.m_CircularBufferMB = this.m_BufferSizeMB;
                return;
            }
        }

        /// <summary>
        /// This is actually a null handle which will log to the NT Kernel Logger when enabled or silently swallow the traced event when it is not running.
        /// See https://www.geoffchappell.com/studies/windows/km/ntoskrnl/api/etw/traceapi/event/index.htm for more information.
        /// </summary>
        internal const int KernelNullSessionHandle = 0;

        /// <summary>
        /// Get the handles of all running kernel trace sessions.
        /// </summary>
        /// <returns>List of kernel trace session handles</returns>
        public static List<ulong> GetKernelSessionHandles()
        {
            List<ulong> sessionIds = new List<ulong>();
            foreach (var sessionName in GetActiveSessionNames())
            {
                try
                {
                    var session = new TraceSession(sessionName);
                    if (session.KernelFlags != 0)
                    {
                        sessionIds.Add(session.SessionHandle);
                    }
                }
                catch (FileNotFoundException)
                {
                    // Ignore not running ETW sessions which are present, but not active
                }
            }

            // Add null session which can also be written by non elevated processes to "NT Kernel Logger" ETW Sessions which is used by xperf as default kernel session.
            if (!sessionIds.Contains(KernelNullSessionHandle))
            {
                sessionIds.Add(KernelNullSessionHandle);
            }

            return sessionIds;
        }

        /// <summary>
        /// ETW trace sessions survive process shutdown. Thus you can attach to existing active sessions.
        /// GetActiveSessionNames() returns a list of currently existing session names.  These can be passed
        /// to the TraceEventSession constructor to open it.   
        /// </summary>
        /// <returns>A enumeration of strings, each of which is a name of a session</returns>
        public unsafe static List<string> GetActiveSessionNames()
        {
            // Allocate structures and two 2048 byte buffers for the session name file name which comes after the structure
            int PropertySize = sizeof(EVENT_TRACE_PROPERTIES) + 2048 + 2048;
            byte* pPropertiesMemory = stackalloc byte[64 * PropertySize];
            EVENT_TRACE_PROPERTIES** pPropertiesPtrs = stackalloc EVENT_TRACE_PROPERTIES*[64];
            for (int i = 0; i < 64; i++)
            {
                EVENT_TRACE_PROPERTIES* pCurProperty = (EVENT_TRACE_PROPERTIES*)(pPropertiesMemory + PropertySize * i);
                pCurProperty->Wnode.BufferSize = (uint)PropertySize;
                pCurProperty->LoggerNameOffset = (uint)sizeof(EVENT_TRACE_PROPERTIES);
                pCurProperty->LogFileNameOffset = (uint)(sizeof(EVENT_TRACE_PROPERTIES) + 2048);
                *(IntPtr*)(pPropertiesPtrs + i) = (IntPtr)pCurProperty;
            }
            int num2 = 0;
            int dwErr = QueryAllTraces((IntPtr)((void*)pPropertiesPtrs), 64, ref num2);
            Marshal.ThrowExceptionForHR(GetHRFromWin32(dwErr));
            List<string> etwSessionNames = new List<string>();
            for (int j = 0; j < num2; j++)
            {
                string item = new string((char*)((byte*)*(pPropertiesPtrs + j) + (*(pPropertiesPtrs + j))->LoggerNameOffset));
                etwSessionNames.Add(item);
            }
            return etwSessionNames;
        }

        private unsafe EVENT_TRACE_PROPERTIES* GetProperties(byte* buffer)
        {
            ZeroMemory((IntPtr)((void*)buffer), TraceSession.PropertiesSize);
            EVENT_TRACE_PROPERTIES* pProperties = (EVENT_TRACE_PROPERTIES*)buffer;
            pProperties->LoggerNameOffset = (uint)sizeof(EVENT_TRACE_PROPERTIES);
            pProperties->LogFileNameOffset = pProperties->LoggerNameOffset + 2048u;
            char* toPtr = (char*)(buffer + pProperties->LoggerNameOffset);
            CopyStringToPtr(toPtr, this.SessionName);
            pProperties->Wnode.BufferSize = (uint)PropertiesSize;
            pProperties->Wnode.Flags = 131072u;
            pProperties->FlushTimer = 60u;
            pProperties->BufferSize = (uint)this.BufferQuantumKB;
            pProperties->MinimumBuffers = (uint)(this.BufferSizeMB * 1024 / this.BufferQuantumKB);
            pProperties->LogFileMode = 134217728u;
            pProperties->LogFileMode = 134217728u;
            if (this.FileName == null)
            {
                pProperties->FlushTimer = 1u;
                if (this.CircularBufferMB == 0)
                {
                    pProperties->LogFileMode = (pProperties->LogFileMode | 256u);
                }
                else
                {
                    pProperties->LogFileMode = (pProperties->LogFileMode | 1024u);
                    pProperties->MinimumBuffers = (uint)(this.CircularBufferMB * 1024 / this.BufferQuantumKB);
                    pProperties->BufferSize = (uint)this.CircularBufferMB;
                }
                pProperties->LogFileNameOffset = 0u;
            }
            else
            {
                if (this.CircularBufferMB != 0)
                {
                    pProperties->LogFileMode = (pProperties->LogFileMode | 2u);
                    pProperties->MaximumFileSize = (uint)this.CircularBufferMB;
                }
                else if (this.MultiFileMB != 0)
                {
                    pProperties->LogFileMode = (pProperties->LogFileMode | 8u);
                    pProperties->MaximumFileSize = (uint)this.MultiFileMB;
                }
                else
                {
                    pProperties->LogFileMode = (pProperties->LogFileMode | 1u);
                }
                if (this.FileName.Length > 1023)
                {
                    throw new ArgumentException("File name too long", "fileName");
                }
                char* toPtr2 = (char*)(buffer + pProperties->LogFileNameOffset);
                CopyStringToPtr(toPtr2, this.FileName);
            }
            pProperties->MaximumBuffers = pProperties->MinimumBuffers * 5u / 4u + 10u;
            pProperties->Wnode.ClientContext = 1u;
            return pProperties;
        }

        // Microsoft.Diagnostics.Tracing.Session.TraceEventSession
        private unsafe static void CopyStringToPtr(char* toPtr, string str)
        {
            fixed (char* ptr = str)
            {
                int i;
                for (i = 0; i < str.Length; i++)
                {
                    toPtr[i] = ptr[i];
                }
                toPtr[i] = '\0';
            }
        }

        internal static int GetHRFromWin32(int dwErr)
        {
            if (dwErr == 0)
            {
                return 0;
            }
            return -2147024896 | (dwErr & 65535);
        }
    }
}


