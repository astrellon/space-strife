using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LysitheaVM.Unity
{
    public class VMRunner : MonoBehaviour
    {
        #region Fields
        public delegate void CompleteHandler(VMRunner runner);
        public delegate void ErrorHandler(VMRunner runner, Exception exp);

        public event CompleteHandler OnComplete;
        public event ErrorHandler OnError;

        public VirtualMachine VM { get; private set; }
        public bool Running;

        public float WaitUntil = -1.0f;
        public float VMStepTiming = -1.0f;
        public int BreakStepsPerFrame = 1_000_000;
        public int MaxStepsPerFrame = 10_000;

        public bool IsWaiting => this.Running && this.WaitUntil > 0.0f;

        private Queue<IFunctionValue> queuedFunctions = new Queue<IFunctionValue>();
        #endregion

        #region Unity Methods
        void Update()
        {
            if (this.VM != null && this.Running)
            {
                var count = 0;
                var runOnce = false;
                var running = this.VM.Running;
                while (this.VM.Running && !this.VM.Paused)
                {
                    if (this.IsWaiting)
                    {
                        this.WaitUntil -= Time.unscaledDeltaTime;
                        if (runOnce || this.WaitUntil > 0.0f)
                        {
                            break;
                        }
                        else
                        {
                            this.WaitUntil = this.VMStepTiming;
                        }
                    }
                    else
                    {
                        this.WaitUntil = this.VMStepTiming;
                    }

                    runOnce = true;

                    // Debug.Log(string.Join('\n', this.VM.PrintStackDebug()));

                    try
                    {
                        this.VM.Step();
                    }
                    catch (VirtualMachineException exp)
                    {
                        this.Running = false;
                        var stackTrace = string.Join("\n -", exp.VirtualMachineStackTrace);
                        Debug.LogError("VM Runner Error: " + exp.Message + "\n- " + stackTrace);
                        this.OnError?.Invoke(this, exp);
                        return;
                    }
                    catch (Exception exp)
                    {
                        this.Running = false;
                        Debug.LogError("Unknown VM Runner Error: " + exp.Message + "\n" + exp.StackTrace);
                        this.OnError?.Invoke(this, exp);
                        return;
                    }
                    count++;

                    if (this.MaxStepsPerFrame > 0 && count > this.MaxStepsPerFrame)
                    {
                        break;
                    }
                    if (this.BreakStepsPerFrame > 0 && count > this.BreakStepsPerFrame)
                    {
                        this.Running = false;
                        Debug.Log("Max step count reached! Breaking!");
                        break;
                    }
                }

                if (running && !this.VM.Running)
                {
                    if (this.queuedFunctions.Count > 0)
                    {
                        this.VM.CallFunction(this.queuedFunctions.Dequeue(), 0, false);
                        this.VM.Running = true;
                    }
                    else
                    {
                        this.Running = false;
                        this.OnComplete?.Invoke(this);
                    }
                }
            }
        }
        #endregion

        #region Methods
        public void QueueFunction(IFunctionValue func)
        {
            this.queuedFunctions.Enqueue(func);
            this.VM.Running = true;
            this.Running = true;
        }

        public void Init(int stackSize)
        {
            this.VM = new VirtualMachine(stackSize);
        }

        public void Init(VirtualMachine vm)
        {
            this.VM = vm;
        }

        public void StartScript(Script script)
        {
            this.Running = true;
            this.VM.Running = true;
            this.VM.Paused = false;
            this.VM.ChangeToScript(script);
        }

        public bool TryGetFunction(string functionName, out IFunctionValue func)
        {
            if (this.VM.BuiltinScope.TryGetKey(functionName, out func))
            {
                return true;
            }
            return this.VM.CurrentScope.TryGetKey(functionName, out func);
        }

        public bool RunFunction(string functionName)
        {
            if (this.TryGetFunction(functionName, out var func))
            {
                this.RunFunction(func);
                return true;
            }
            else
            {
                Debug.LogWarning($"Unable to find script function {functionName}");
                return false;
            }
        }

        public void RunFunction(IFunctionValue func, params IValue[] args)
        {
            this.Running = true;
            this.VM.Running = true;
            this.VM.Paused = false;

            var argsValue = args.Length > 0 ? new ArgumentsValue(args) : ArgumentsValue.Empty;
            func.Invoke(this.VM, argsValue, true);
        }

        public void Wait(TimeSpan timespan)
        {
            this.WaitUntil = (float)timespan.TotalSeconds;
        }
        #endregion
    }
}