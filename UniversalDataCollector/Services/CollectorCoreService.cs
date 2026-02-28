using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using UniversalDataCollector.Models;

namespace UniversalDataCollector.Services
{
    /// <summary>
    /// 核心采集引擎：负责协调扫描、读取、解析、上传和备份
    /// </summary>
    public class CollectorCoreService
    {
        // 依赖服务
        private readonly MonitorService _monitorService = new MonitorService();

        private readonly MesService _mesService = new MesService();
        private readonly ConfigService _configService = new ConfigService();
        private readonly DataMappingService _mappingService = new DataMappingService();

        // ★ 新增：模板增强服务
        private readonly TemplateEnricherService _templateService = new TemplateEnricherService();

        // 状态变量
        private MonitorConfig _monitorConfig;

        private AppConfig _mesConfig;
        private DispatcherTimer _timer;
        private bool _isRunning = false;
        private object _ioLock = new object();

        // 运行状态记录
        private int _currentRowIndex = 0;

        private string _activeSourceFile = "";
        private string _currentExportPath = "";
        private HashSet<string> _processedFolderFiles = new HashSet<string>();

        private const string _singleFileCache = "SingleMode_Row.cache";
        private const string _folderHistory = "FolderMode_History.db";

        // 事件：通知 ViewModel 更新界面
        public event Action<string> OnLog;

        public event Action<string> OnStatusChange;

        public event Action<Dictionary<string, object>> OnNewDataProcessed;

        public CollectorCoreService()
        {
            _timer = new DispatcherTimer();
            _timer.Tick += OnTimerTick;
        }

        public void Start()
        {
            if (_isRunning) return;

            try
            {
                // 1. 加载主配置
                _mesConfig = _configService.Load<AppConfig>("AppConfig.json") ?? new AppConfig();
                _monitorConfig = _configService.Load<MonitorConfig>("MonitorConfig.json") ?? new MonitorConfig();

                // 2. 加载模板服务配置
                _templateService.LoadConfig();

                // 3. 加载历史记录
                LoadHistory();

                // 4. 启动定时器
                _timer.Interval = TimeSpan.FromSeconds(_monitorConfig.IntervalSeconds > 0 ? _monitorConfig.IntervalSeconds : 3);
                _timer.Start();
                _isRunning = true;

                Log("系统引擎已启动，监控模式: " + _monitorConfig.Mode);
                Status("运行中...");
            }
            catch (Exception ex)
            {
                Log("启动失败: " + ex.Message);
            }
        }

        public void Stop()
        {
            _timer.Stop();
            _isRunning = false;
            Status("已停止");
            Log("系统已停止");
        }

        private async void OnTimerTick(object sender, EventArgs e)
        {
            _timer.Stop();
            try
            {
                if (_monitorConfig.Mode == MonitorMode.File) await ProcessSingleFileMode();
                else await ProcessFolderMode();
            }
            catch (Exception ex)
            {
                Log("运行异常: " + ex.Message);
            }
            finally
            {
                if (_isRunning) _timer.Start();
            }
        }

        private async Task ProcessSingleFileMode()
        {
            string path = _monitorConfig.TargetFilePath;
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

            if (_activeSourceFile != path)
            {
                _activeSourceFile = path;
                _currentRowIndex = File.Exists(_singleFileCache) ? int.Parse(File.ReadAllText(_singleFileCache)) : 0;
                _currentExportPath = PrepareExportPath(path);
                Log("锁定单文件: " + Path.GetFileName(path));
            }
            await ReadAndUpload(path);
        }

        private async Task ProcessFolderMode()
        {
            var files = _monitorService.GetMatchedFiles(_monitorConfig);
            string targetFile = files.FirstOrDefault(f => !_processedFolderFiles.Contains(f));

            if (string.IsNullOrEmpty(targetFile))
            {
                Status("监控中 - 等待新文件...");
                return;
            }

            if (_activeSourceFile != targetFile)
            {
                _activeSourceFile = targetFile;
                _currentRowIndex = 0;
                _currentExportPath = PrepareExportPath(targetFile);
                Log("发现新文件: " + Path.GetFileName(targetFile));
            }

            bool hasNewData = await ReadAndUpload(targetFile);

            if (!hasNewData)
            {
                if (!_processedFolderFiles.Contains(targetFile))
                {
                    MarkFileAsProcessed(targetFile);
                    Log("文件处理完毕: " + Path.GetFileName(targetFile));
                }
            }
        }

        private async Task<bool> ReadAndUpload(string path)
        {
            var res = _monitorService.ReadFileContent(_monitorConfig, path, _currentRowIndex);

            if (res.NewRows.Count > 0)
            {
                Status("正在上传: " + Path.GetFileName(path));
                foreach (var row in res.NewRows)
                {
                    // 1. 解析 CSV 数据 (得到扁平字典)
                    var flatData = _mappingService.MapRow(row.Columns, _mesConfig.Mappings);

                    // ★★★ 2. 动态模板数据融合 ★★★
                    // 查找是否有被配置为 "TemplateFile" 的列
                    if (_mesConfig.Mappings != null)
                    {
                        var templateMap = _mesConfig.Mappings.FirstOrDefault(m => m.DataType == "TemplateFile");
                        if (templateMap != null)
                        {
                            // 从扁平数据中拿出文件名 (Key = MesFieldName)
                            if (flatData.TryGetValue(templateMap.MesFieldName, out object fileNameObj))
                            {
                                string fileName = fileNameObj?.ToString();

                                // 调用增强服务，传入文件名获取参数
                                var templateParams = _templateService.Enrich(fileName);

                                // 将模板参数合并入扁平数据 (作为新列)
                                foreach (var kvp in templateParams)
                                {
                                    flatData[kvp.Key] = kvp.Value;
                                }
                            }
                        }
                    }

                    // 3. 构造嵌套结构 (MES结构) - 此时 flatData 已包含模板新字段
                    var mesData = _mappingService.BuildNestedData(flatData);

                    // 4. 上传
                    bool success = await _mesService.UploadDynamicAsync(_mesConfig.MesApiUrl, mesData, Log);

                    if (success)
                    {
                        _currentRowIndex = row.LineIndex;
                        SaveState();

                        // 本地备份和界面显示都会看到新增加的列
                        WriteToLocalCsv(flatData);
                        OnNewDataProcessed?.Invoke(flatData);
                    }
                    else
                    {
                        return true;
                    }
                }
                return true;
            }
            return false;
        }

        // --- 辅助方法 ---

        private void LoadHistory()
        {
            if (File.Exists(_folderHistory))
            {
                var lines = File.ReadAllLines(_folderHistory);
                foreach (var l in lines) if (!string.IsNullOrEmpty(l)) _processedFolderFiles.Add(l);
            }
        }

        private void MarkFileAsProcessed(string file)
        {
            _processedFolderFiles.Add(file);
            lock (_ioLock) { File.AppendAllLines(_folderHistory, new[] { file }); }
        }

        private void SaveState()
        {
            if (_monitorConfig.Mode == MonitorMode.File)
            {
                File.WriteAllText(_singleFileCache, _currentRowIndex.ToString());
            }
        }

        private string PrepareExportPath(string sourcePath)
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ExportData");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string name = Path.GetFileNameWithoutExtension(sourcePath);
            return Path.Combine(dir, $"{name}_{DateTime.Now:yyyyMMdd}.csv");
        }

        private void WriteToLocalCsv(Dictionary<string, object> data)
        {
            if (string.IsNullOrEmpty(_currentExportPath)) return;
            lock (_ioLock)
            {
                try
                {
                    bool isNew = !File.Exists(_currentExportPath);
                    using (var sw = new StreamWriter(_currentExportPath, true, new UTF8Encoding(true)))
                    {
                        if (isNew) sw.WriteLine("Time," + string.Join(",", data.Keys));

                        var values = data.Values.Select(v =>
                        {
                            if (v is System.Collections.IEnumerable list && !(v is string))
                                return "\"" + string.Join(";", ((IEnumerable<object>)list).Cast<object>()) + "\"";
                            return "\"" + v + "\"";
                        });

                        sw.WriteLine(DateTime.Now.ToString("HH:mm:ss") + "," + string.Join(",", values));
                    }
                }
                catch { }
            }
        }

        private void Log(string msg) => OnLog?.Invoke(msg);

        private void Status(string msg) => OnStatusChange?.Invoke(msg);
    }
}