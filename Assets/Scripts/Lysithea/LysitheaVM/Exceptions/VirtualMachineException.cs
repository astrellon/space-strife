using System;
using System.Collections.Generic;

#nullable enable

namespace LysitheaVM
{
    public class VirtualMachineException : Exception
    {
        #region Fields
        public readonly VirtualMachine VM;
        public readonly IReadOnlyList<string> VirtualMachineStackTrace;
        public readonly string OriginalMessage;
        #endregion

        #region Constructor
        public VirtualMachineException(VirtualMachine vm, IReadOnlyList<string> virtualMachineStackTrace, string message) : base(message + "\n" + string.Join("\n", virtualMachineStackTrace))
        {
            this.VM = vm;
            this.VirtualMachineStackTrace = virtualMachineStackTrace;
            this.OriginalMessage = message;
        }

        public VirtualMachineException(Exception innerException, VirtualMachine vm, IReadOnlyList<string> virtualMachineStackTrace, string message) : base(message + "\n" + string.Join("\n", virtualMachineStackTrace), innerException)
        {
            this.VM = vm;
            this.VirtualMachineStackTrace = virtualMachineStackTrace;
            this.OriginalMessage = message;
        }
        #endregion
    }
}