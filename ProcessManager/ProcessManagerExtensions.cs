﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Linux
{
    public static class ProcessManagerExtensions
    {
        public static int[] GetProcessIds(this IProcessManager processManager)
        {
            return processManager.EnumerateProcessIds().ToArray();
        }

        public static ProcessInfo GetProcessInfoById(this IProcessManager processManager, int pid)
        {
            return processManager.GetProcessInfos(new[] {pid}).FirstOrDefault();
        }

        public static ProcessInfo[] GetProcessInfos(this IProcessManager processManager)
        {
            var processIds = processManager.GetProcessIds();
            return processManager.GetProcessInfos(processIds);
        }

        public static ProcessInfo[] GetProcessInfos(this IProcessManager processManager, int[] processIds)
        {
            return processManager.GetProcessInfos(processIds, (info) => true);
        }

        public static ProcessInfo[] GetProcessInfos(this IProcessManager processManager,
            Func<ProcessInfo, bool> predicate)
        {
            return processManager.GetProcessInfos(processManager.EnumerateProcessIds(), predicate);
        }

        public static void Kill(this IProcessManager processManager, int pid, ProcessSignal signal)
        {
            processManager.Kill(pid, (int)signal);
        }

        public static bool TryKill(this IProcessManager processManager, int pid, ProcessSignal signal)
        {
            try
            {
                processManager.Kill(pid, (int)signal);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void Kill(this IProcessManager processManager, string processName, string userName,
            ProcessSignal signal = ProcessSignal.SIGTERM,
            Action<Exception> onError = null)
        {
            var result = Syscall.GetPasswdByUserName(userName);
            if (string.IsNullOrWhiteSpace(result.pw_name))
            {
                throw new Win32Exception($"Not found user '{userName}'");
            }

            processManager.Kill(processName, result.pw_uid, signal, onError);
        }
        
        public static void Kill(this IProcessManager processManager, string processName, uint uid,
            ProcessSignal signal = ProcessSignal.SIGTERM,
            Action<Exception> onError = null)
        {          
            processManager.Kill(
                _ => (_.ProcessName == processName || (_.ExecutablePath!=null && _.ExecutablePath.EndsWith(processName))) && (_.Euid == uid || _.Ruid == uid), 
                signal,
                onError);
            
        }
        
        public static void Kill(this IProcessManager processManager, Func<ProcessInfo, bool> predicate,
            ProcessSignal signal = ProcessSignal.SIGTERM,
            Action<Exception> onError = null)
        {          
            processManager
                .GetProcessInfos(predicate)
                .ForEach(_ =>
                {
                    try
                    {
                        processManager.Kill(_.ProcessId, signal);
                    }
                    catch (Exception e)
                    {
                        onError?.Invoke(e);
                    }
                })
                ;
            
        }

        public static IDictionary<string, string> GetEnvironmentVariables(this IProcessManager processManager, int pid)
        {
            return processManager.GetEnvironmentVariables(pid, null);
        }
        
        public static string GetEnvironmentVariable(this IProcessManager processManager, int pid, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            var found = processManager.GetEnvironmentVariables(pid,
                _ => name.Equals(_.Key, StringComparison.OrdinalIgnoreCase));
            return found.Any() ? found.Single().Value : string.Empty;
        }
    }
}