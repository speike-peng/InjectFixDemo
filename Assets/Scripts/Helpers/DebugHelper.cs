using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using UnityEngine;

public class DebugHelper
{
    private static readonly int MainThreadId = Thread.CurrentThread.ManagedThreadId;
    public static bool IsMainThread
    {
        get { return Thread.CurrentThread.ManagedThreadId == MainThreadId; }
    }
    private static readonly StringBuilder GameLog = new StringBuilder();
    private static readonly StringBuilder StackTraceStrBuilder = new StringBuilder();
    private static bool mNeedPrint = false;


    /// <summary>
    /// 打印日志，只在ENABLE_LOG宏下作用，一般用于开发时使用
    /// </summary>
    [Conditional("ENABLE_LOG")]
    public static void Log(string message)
    {
        message = $"[L]{DateTime.Now:HH:mm:ss:fff}       {message}";

        if (IsMainThread)
        {
            UnityEngine.Debug.Log(message);
        }
        else
        {
            lock (GameLog)
            {
                message = $"Not Main Thread -->       {message}\n {GetStackTrace(2)}";
                GameLog.Append(message);
                mNeedPrint = true;
            }
        }
    }

    /// <summary>
    /// 打印日志，只在ENABLE_LOG宏下作用，一般用于开发时使用
    /// </summary>
    [Conditional("ENABLE_LOG")]
    public static void Log(string format, params object[] aArgs)
    {
        Log(string.Format(format, aArgs));
    }

    /// <summary>
    /// 打印警告，只在ENABLE_LOG宏下作用，一般用于开发时使用
    /// </summary>
    [Conditional("ENABLE_LOG")]
    public static void LogWarning(string message)
    {
        message = $"[W]{DateTime.Now:HH:mm:ss:fff}       {message}";

        if (IsMainThread)
        {
            UnityEngine.Debug.LogWarning(message);
        }
        else
        {
            RecordOtherThreadGameLog(message);
        }
    }

    /// <summary>
    /// 打印警告，只在ENABLE_LOG宏下作用，一般用于开发时使用
    /// </summary>
    [Conditional("ENABLE_LOG")]
    public static void LogWarning(string format, params object[] aArgs)
    {
        LogWarning(string.Format(format, aArgs));
    }

    /// <summary>
    /// 打印错误，触发Bugly上报
    /// </summary>
    public static void LogError(string message)
    {
        message = $"[E]{DateTime.Now:HH:mm:ss:fff}       {message}";

        if (IsMainThread)
        {
            UnityEngine.Debug.LogError(message);
        }
        else
        {
            RecordOtherThreadGameLog(message);
        }
    }

    /// <summary>
    /// 打印错误，触发Bugly上报
    /// </summary>
    public static void LogError(Exception e)
    {
        LogError(e.ToString());
    }


    /// <summary>
    /// 打印错误，触发Bugly上报
    /// </summary>
    public static void LogError(string format, params object[] aArgs)
    {
        LogError(string.Format(format, aArgs));
    }

    /// <summary>
    /// 记录关键信息，关键流程，如果发生错误会随错误上报Bugly
    /// </summary>
    public static void LogEvent(string message)
    {
        message = $"[K]{DateTime.Now:HH:mm:ss:fff}       {message}";

        if (IsMainThread)
        {
            UnityEngine.Debug.Log(message);
            //if (BuglyAgent.IsInitialized)
            //    BuglyAgent.PrintLog(LogSeverity.LogWarning, "{0}", message); // iOS自定义日志LogWarning才上报
        }
        else
        {
            RecordOtherThreadGameLog(message);
        }
    }

    private static void RecordOtherThreadGameLog(string message)
    {
        lock (GameLog)
        {
            message = $"Not Main Thread -->       {message}\n {GetStackTrace(2)}";
            GameLog.Append(message);
            mNeedPrint = true;
        }
    }

    private static string GetStackTrace(int aRemove)
    {
        StackTrace st = new StackTrace(true);
        StackFrame[] sf = st.GetFrames();

        if (null == sf || sf.Length <= 2)
            return "    GetStackTrace Failed";

        StackTraceStrBuilder.Length = 0;

        for (int i = aRemove; i < sf.Length; ++i)
        {
            if (null == sf[i])
            {
                StackTraceStrBuilder.Append("    null\n");

                continue;
            }

            var declaringType = sf[i].GetMethod().ReflectedType;

            StackTraceStrBuilder.AppendFormat("    {0}:{1} ({2}:{3})\n", null != declaringType ? declaringType.FullName : null, sf[i].GetMethod().Name,
                sf[i].GetFileName(), sf[i].GetFileLineNumber());
        }

        return StackTraceStrBuilder.ToString();
    }

}
