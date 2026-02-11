using System;
using System.Threading;
using System.Windows;
using System.Diagnostics;
using System.Reflection;

namespace UniversalDataCollector
{
    public partial class App : Application
    {
        // 使用唯一的 GUID 作为锁名称，防止与其他程序冲突
        private const string MutexName = "Global\\UniversalDataCollector_2024_Unique_Mutex";

        private static Mutex _mutex = null;

        protected override void OnStartup(StartupEventArgs e)
        {
            _mutex = new Mutex(true, MutexName, out bool createdNew);

            // 如果锁已被占用，进入“重启/多开”检测逻辑
            if (!createdNew)
            {
                // ★ 关键修复：给重启留出 2 秒的重试时间 ★
                // 这样新进程会静默等待旧进程“咽气”，而不是直接弹窗
                try
                {
                    if (!_mutex.WaitOne(TimeSpan.FromSeconds(2), false))
                    {
                        MessageBox.Show("程序已在运行中，请检查任务栏或任务管理器。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        Application.Current.Shutdown();
                        return;
                    }
                }
                catch (AbandonedMutexException)
                {
                    // 如果旧进程崩溃了没释放锁，这里捕获后依然可以继续运行
                }
            }

            base.OnStartup(e);
        }

        /// <summary>
        /// 提供给 ViewModel 调用的安全重启方法
        /// </summary>
        public static void RequestRestart()
        {
            // 1. 手动释放并关闭锁，让出坑位
            if (_mutex != null)
            {
                try { _mutex.ReleaseMutex(); } catch { }
                _mutex.Dispose();
                _mutex = null;
            }

            // 2. 启动新实例
            Process.Start(Assembly.GetEntryAssembly().Location);

            // 3. 彻底杀死当前进程
            Environment.Exit(0);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_mutex != null)
            {
                try { _mutex.ReleaseMutex(); } catch { }
                _mutex.Dispose();
            }
            base.OnExit(e);
        }
    }
}