using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using UtfUnknown;
using VisualGrep.Models;
using VisualGrep.Utls;
using System.Reactive.Linq;
using System.Threading;
using System.Windows.Documents;
using System.Windows.Media;
using ExcelDataReader;
using System.Data;
using System.Diagnostics;
using Microsoft.WindowsAPICodePack.Dialogs;
using MahApps.Metro.Controls.Dialogs;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace VisualGrep.ViewModels
{
    public class MainWindowViewModel
    {
        public ReactiveProperty<string> FolderPath { get; } = new ReactiveProperty<string>(string.Empty);
        public ReactiveProperty<string> SearchText { get; } = new ReactiveProperty<string>(string.Empty);
        public ReactiveProperty<string> SearchFileName { get; } = new ReactiveProperty<string>(string.Empty);
        public ReadOnlyReactiveProperty<string?> SearchTextWatermark { get; }
        public ReactiveProperty<bool> SearchEnable { get; }
        public ReadOnlyReactiveProperty<bool> SearchStopEnable { get; }
        public ReactiveProperty<bool> IncludeSubfolders { get; } = new ReactiveProperty<bool>(true);
        public ReactiveProperty<bool> UseRegex { get; } = new ReactiveProperty<bool>(false);
        public ReactiveProperty<bool> CaseSensitive { get; } = new ReactiveProperty<bool>(false);
        public ReactiveCommand SearchCommand { get; } = new ReactiveCommand();
        public ReactiveCommand StopCommand { get; } = new ReactiveCommand();
        public ReactiveCommand<DragEventArgs> DropCommand { get; } = new ReactiveCommand<DragEventArgs>();
        public ReactiveCommand<DragEventArgs> PreviewDragOverCommand { get; } = new ReactiveCommand<DragEventArgs>();
        public ReactiveProperty<LineInfo> SelectedLineInfo { get; } = new ReactiveProperty<LineInfo>();
        public ReactiveCommand<MouseButtonEventArgs> OpenFileCommand { get; } = new ReactiveCommand<MouseButtonEventArgs>();
        public ReactiveCommand<SelectionChangedEventArgs> LineInfoSelectionChanged { get; } = new ReactiveCommand<SelectionChangedEventArgs>();
        public ObservableCollection<LineInfo> LineInfoList { get; } = new ObservableCollection<LineInfo>();
        public ObservableCollection<TabPanelViewModel> TabPanels { get; } = new ObservableCollection<TabPanelViewModel>();
        public ReactiveProperty<string> SearchFilePath { get; } = new ReactiveProperty<string>(string.Empty);
        public ReactiveProperty<List<RichTextItem>> OutMessage { get; } = new ReactiveProperty<List<RichTextItem>>();
        private CancellationTokenSource _CancellationTokenSource = new CancellationTokenSource();
        public ReadOnlyReactiveProperty<bool> ControlEnable { get; }
        public ReactiveProperty<bool> SearchingFlag { get; } = new ReactiveProperty<bool>(false);
        public ReactiveProperty<Visibility> TextPanelVisibility { get; } = new ReactiveProperty<Visibility>(Visibility.Visible);
        public ReactiveProperty<Visibility> ExcelPanelVisibility { get; }
        public ReactiveCommand LineInfoListOutputCommand { get; } = new ReactiveCommand();
        public ReactiveProperty<bool> LineInfoListOutputEnabled { get; } = new ReactiveProperty<bool>(false);
        public ObservableCollection<OutputTypeViewModel> OutputTypeList { get; } = new ObservableCollection<OutputTypeViewModel>();
        public ReactiveProperty<OutputTypeViewModel> SelectedOutputType { get; } = new ReactiveProperty<OutputTypeViewModel>();
        public ReactiveProperty<bool> CombineMatches { get; } = new ReactiveProperty<bool>(true);
        public ReactiveCommand FolderOpenCommand { get; } = new ReactiveCommand();
        public ReactiveCommand CsvOutputCommand { get; } = new ReactiveCommand();
        public ReactiveCommand TsvOutputCommand { get; } = new ReactiveCommand();
        public IDialogCoordinator? MahAppsDialogCoordinator { get; set; }
        public ObservableCollection<string> SearchHistory { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> SearchDirectoryHistory { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> SearchFileNameHistory { get; } = new ObservableCollection<string>();
        public ReactiveCommand ClosingCommand { get; } = new ReactiveCommand();
        public ReactiveCommand ClearHistoryCommand { get; } = new ReactiveCommand();
        public ReactiveProperty<int> Counter { get; } = new ReactiveProperty<int>(0);
        public ReactiveProperty<int> Maximum { get; } = new ReactiveProperty<int>(100);
        public ReactiveProperty<string> SearchingInfo { get; } = new ReactiveProperty<string>();
        public ReactiveProperty<string> SearchingInfoPercent { get; } = new ReactiveProperty<string>();
        public ReactiveProperty<Visibility> SearchingInfoVisibility { get; } = new ReactiveProperty<Visibility>(Visibility.Collapsed);
        public ReactiveProperty<Visibility> SearchingResultInfoVisibility { get; }
        private string _OutputFolderPath;
        private Stopwatch _Stopwatch = new Stopwatch();
        public ReactiveProperty<string> SearchResultInfo { get; } = new ReactiveProperty<string>();
        // ログを出力する変数定義
        static private Logger logger = LogManager.GetCurrentClassLogger();
        public ReactiveCommand OpenFileFolderCommand { get; } = new ReactiveCommand();
        public ReactiveCommand ClipboardCopyFileFullPathCommand { get; } = new ReactiveCommand();
        public ReactiveCommand ClipboardCopyFileNameCommand { get; } = new ReactiveCommand();
        public ReactiveCommand ClipboardCopyFileFolderPathCommand { get; } = new ReactiveCommand();
        public MainWindowViewModel()
        {
            LogManager.Setup().LoadConfiguration(builder =>
            {
                builder.ForLogger().FilterMinLevel(LogLevel.Debug).WriteToConsole();
                builder.ForLogger().FilterMinLevel(LogLevel.Info).WriteToFile(fileName: "./logs/${processname}.log");
                builder.ForLogger().FilterMinLevel(LogLevel.Error).WriteToFile(fileName: "./logs/${processname}.log");
            });

            LoadHistory();

            SearchingResultInfoVisibility = SearchingInfoVisibility.Select(x => x == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible).ToReactiveProperty();

            BindingOperations.EnableCollectionSynchronization(LineInfoList, new object());

            SearchEnable = FolderPath.CombineLatest(SearchingFlag, (path, y) => !string.IsNullOrEmpty(path) && !y).ToReactiveProperty();

            SearchTextWatermark = UseRegex.Select(x => x ? "正規表現" : "検索文字列").ToReadOnlyReactiveProperty();

            ControlEnable = SearchingFlag.Select(x => !x).ToReadOnlyReactiveProperty();

            SearchStopEnable = SearchingFlag.Select(x => x).ToReadOnlyReactiveProperty();

            ExcelPanelVisibility = TextPanelVisibility.Select(x => x == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible).ToReactiveProperty();

            var outputTypeViewModelList = Enum.GetValues(typeof(OutputType)).OfType<OutputType>().Select(x => new OutputTypeViewModel(x)).ToList();

            foreach (var vm in outputTypeViewModelList)
            {
                OutputTypeList.Add(vm);
            }

            SelectedOutputType.Value = outputTypeViewModelList.First();

            SearchCommand.Subscribe(async e =>
            {
                try
                {
                    OutMessage.Value = new List<RichTextItem>();

                    if (string.IsNullOrEmpty(SearchText.Value))
                    {
                        return;
                    }

                    if (string.IsNullOrEmpty(FolderPath.Value))
                    {
                        return;
                    }

                    if (!Directory.Exists(FolderPath.Value))
                    {
                        return;
                    }

                    if (!SearchHistory.Contains(SearchText.Value))
                    {
                        SearchHistory.Add(SearchText.Value);
                    }

                    if (!SearchDirectoryHistory.Contains(FolderPath.Value))
                    {
                        SearchDirectoryHistory.Add(FolderPath.Value);
                    }

                    if (!string.IsNullOrEmpty(SearchFileName.Value) && !SearchFileNameHistory.Contains(SearchFileName.Value))
                    {
                        SearchFileNameHistory.Add(SearchFileName.Value);
                    }

                    _Stopwatch = new Stopwatch();

                    _Stopwatch.Start();

                    LineInfoList.Clear();

                    SearchingFlag.Value = true;

                    SearchingInfoVisibility.Value = Visibility.Visible;

                    var source = Observable.Interval(TimeSpan.FromMilliseconds(500));

                    var subscription = source.Subscribe(
                        i => UpdateElapsed(),
                        ex => Console.WriteLine("OnError({0})", ex.Message),
                        () => Console.WriteLine("Completed()"));

                    var files = FileUtils.GetAllFiles(FolderPath.Value, IncludeSubfolders.Value)
                        .Select((value, index) => value)
                        .Where(x => SearchFileName.Value == string.Empty ? true : x.Contains(SearchFileName.Value))
                        .ToList();

                    Maximum.Value = files.Count;
                    Counter.Value = 0;

                    try
                    {
                        await Task.Run(async () =>
                        {
                            foreach (var file in files)
                            {
                                SearchFilePath.Value = file;
                                var list = await SearchFile(file, SearchText.Value, _CancellationTokenSource.Token);
                                LineInfoList.AddAllSafe(list);
                                Counter.Value = Counter.Value + 1;
                            }
                        });
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    finally
                    {
                        SearchFilePath.Value = string.Empty;

                        _CancellationTokenSource = new CancellationTokenSource();

                        var tempList = LineInfoList.OrderBy(x => x.FileName).ThenBy(x => x.FilePath).ThenBy(x => long.Parse(x.Line)).ToList();

                        LineInfoList.ClearAndAddAllSafe(tempList);

                        LineInfoListOutputEnabled.Value = LineInfoList.Any();

                        _Stopwatch.Stop();

                        SearchResultInfo.Value = $"終了しました。(検索ファイル数 {Maximum.Value} 検索時間 {_Stopwatch.Elapsed:hh\\:mm\\:ss})";

                        SearchingFlag.Value = false;

                        subscription.Dispose();

                        SearchingInfoVisibility.Value = Visibility.Collapsed;
                    }

                }
                catch (Exception ex)
                {
                    logger.Error("SearchCommand Error " + ex.Message);
                }
            });

            StopCommand.Subscribe(e =>
            {
                _CancellationTokenSource.Cancel();
            });

            DropCommand.Subscribe(e =>
            {
                var files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (files.Any())
                {
                    var path = files.First();
                    var dir = Directory.Exists(path) ? path : Path.GetDirectoryName(path);

                    if (dir != null)
                    {
                        FolderPath.Value = dir;
                    }

                    var extension = Path.GetExtension(files.First());

                    if (!string.IsNullOrEmpty(extension))
                    {
                        SearchFileName.Value = extension;
                    }
                }
            });

            PreviewDragOverCommand.Subscribe(e =>
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = e.Data.GetDataPresent(DataFormats.FileDrop);
            });

            OpenFileCommand.Subscribe(e =>
            {
                var proc = new Process();

                if (SelectedLineInfo.Value == null) return;

                proc.StartInfo.FileName = SelectedLineInfo.Value.FullPath;
                proc.StartInfo.UseShellExecute = true;
                proc.Start();
            });

            LineInfoSelectionChanged.Subscribe(async e =>
            {
                var list = new List<RichTextItem>();

                var grid = e.Source as DataGrid;
                var info = grid?.SelectedItem as LineInfo;

                if (info == null)
                {
                    return;
                }

                var ext = Path.GetExtension(info.FullPath);

                if (ext == ".txt" || ext == ".csv" || ext == ".tsv" || ext == ".log" || ext == ".xml" || ext == ".json" || ext == ".html" || ext == ".md")
                {
                    TextPanelVisibility.Value = Visibility.Visible;
                    if (info != null)
                    {
                        await ReadFile(info.FullPath, (line) =>
                        {
                            var matchresult = MatchText(line, SearchText.Value, UseRegex.Value, !CaseSensitive.Value);
                            var text = new RichTextItem();
                            text.Text = line ?? string.Empty;
                            text.Foreground = matchresult.Any() ? Brushes.Red : Brushes.Black;

                            list.Add(text);
                        });

                        OutMessage.Value = list;
                    }
                }
                else if (ext == ".xls" || ext == ".xlsx" || ext == ".xlsb")
                {
                    TextPanelVisibility.Value = Visibility.Collapsed;
                    TabPanels.Clear();
                    Action<DataTable> action = (worksheet) =>
                    {
                        var panel = new TabPanelViewModel();
                        panel.Header.Value = worksheet.TableName;

                        var list = new List<RichTextItem>();

                        //セルの入力文字を読み取り
                        for (var row = 0; row < worksheet.Rows.Count; row++)
                        {
                            var text = new RichTextItem();
                            var line = string.Empty;

                            for (var col = 0; col < worksheet.Columns.Count; col++)
                            {
                                var cell = worksheet.Rows[row][col];
                                line += cell?.ToString() ?? "";
                            }

                            text.Text = line;
                            var matchresult = MatchText(line, SearchText.Value, UseRegex.Value, !CaseSensitive.Value);
                            text.Foreground = matchresult.Any() ? Brushes.Red : Brushes.Black;
                            list.Add(text);
                        }

                        panel.OutMessage.Value = list;

                        TabPanels.Add(panel);
                    };

                    ReadExcel(info.FullPath, action);
                }
                else
                {
                    DetectionResult? charsetDetectedResult = null;
                    using (var stream = new FileStream(info.FullPath, FileMode.Open))
                    {
                        charsetDetectedResult = DetectFromStream(stream);
                    }

                    if (charsetDetectedResult.Detected is not null)
                    {
                        TextPanelVisibility.Value = Visibility.Visible;
                        if (info != null)
                        {
                            await ReadFile(info.FullPath, (line) =>
                            {
                                var matchresult = MatchText(line, SearchText.Value, UseRegex.Value, !CaseSensitive.Value);
                                var text = new RichTextItem();
                                text.Text = line ?? string.Empty;
                                text.Foreground = matchresult.Any() ? Brushes.Red : Brushes.Black;

                                list.Add(text);
                            });

                            OutMessage.Value = list;
                        }
                    }
                }
            });

            LineInfoListOutputCommand.Subscribe(e =>
            {
                try
                {
                    var name = Path.GetTempFileName();
                    using (StreamWriter sw = new StreamWriter(name, false, Encoding.UTF8))
                    {
                        foreach (var line in GetContentLines())
                        {
                            sw.WriteLine(line);
                        }
                    }

                    var proc = new System.Diagnostics.Process();

                    proc.StartInfo.FileName = name;
                    proc.StartInfo.UseShellExecute = true;
                    proc.Start();
                }
                catch (Exception ex)
                {
                    logger.Error("LineInfoListOutputCommand Error " + ex.Message);
                }
            });

            FolderOpenCommand.Subscribe(e =>
            {
                try
                {
                    var path = SelectPath(true, initialDirectory: string.IsNullOrEmpty(FolderPath.Value) ? null : FolderPath.Value);

                    if (path is not null)
                    {
                        FolderPath.Value = path;
                    }
                }
                catch (Exception ex)
                {
                    logger.Error("FolderOpenCommand Error " + ex.Message);
                }
            });

            CsvOutputCommand.Subscribe(async e =>
            {
                try
                {
                    var path = SelectPath(extenstion: ".csv", initialDirectory: string.IsNullOrEmpty(_OutputFolderPath) ? null : _OutputFolderPath);
                    if (path != null)
                    {
                        _OutputFolderPath = Path.GetDirectoryName(path);
                        using (StreamWriter sw = new StreamWriter(path, false, Encoding.UTF8))
                        {
                            foreach (var line in GetContentLines(Format.Csv))
                            {
                                sw.WriteLine(line);
                            }
                        }

                        if (MahAppsDialogCoordinator is not null)
                        {
                            await MahAppsDialogCoordinator.ShowMessageAsync(this, "CSV出力", $"{path}に出力しました。");
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error("CsvOutputCommand Error " + ex.Message);
                }
            });
            TsvOutputCommand.Subscribe(async e =>
            {
                try
                {
                    var path = SelectPath(extenstion: ".tsv", initialDirectory: string.IsNullOrEmpty(_OutputFolderPath) ? null : _OutputFolderPath);
                    if (path != null)
                    {
                        _OutputFolderPath = Path.GetDirectoryName(path);
                        using (StreamWriter sw = new StreamWriter(path, false, Encoding.UTF8))
                        {
                            foreach (var line in GetContentLines(Format.Tsv))
                            {
                                sw.WriteLine(line);
                            }
                        }
                        if (MahAppsDialogCoordinator is not null)
                        {
                            await MahAppsDialogCoordinator.ShowMessageAsync(this, "TSV出力", $"{path}に出力しました。");
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error("TsvOutputCommand Error " + ex.Message);
                }
            });

            ClosingCommand.Subscribe(e =>
            {
                SaveHistory();
            });

            ClearHistoryCommand.Subscribe(async e =>
            {
                var metroDialogSettings = new MetroDialogSettings()
                {
                    AffirmativeButtonText = "はい",
                    NegativeButtonText = "いいえ",
                    AnimateHide = true,
                    AnimateShow = true,
                    ColorScheme = MetroDialogColorScheme.Theme,
                };

                if (MahAppsDialogCoordinator == null)
                {
                    return;
                }

                var diagResult = await MahAppsDialogCoordinator.ShowMessageAsync(this, "履歴クリア", "検索履歴を削除します。よろしいですか？", MessageDialogStyle.AffirmativeAndNegative, settings: metroDialogSettings);
                if (diagResult == MessageDialogResult.Affirmative)
                {
                    SearchHistory.Clear();
                    SearchDirectoryHistory.Clear();
                    SearchFileNameHistory.Clear();
                    SaveHistory();
                }
            });

            OpenFileFolderCommand.Subscribe(e =>
            {
                var path = SelectedLineInfo.Value.FilePath;
                Process.Start(path);
            });

            ClipboardCopyFileFullPathCommand.Subscribe(e =>
            {
                var path = SelectedLineInfo.Value.FullPath;
                Clipboard.SetText(path);
            });

            ClipboardCopyFileNameCommand.Subscribe(e =>
            {
                var fileName = SelectedLineInfo.Value.FileName;
                Clipboard.SetText(fileName);
            });

            ClipboardCopyFileFolderPathCommand.Subscribe(e =>
            {
                var path =  SelectedLineInfo.Value.FilePath;
                Clipboard.SetText(path);
            });
        }

        private string? SelectPath(bool isFolderPicker = false, string? extenstion = null, string? initialDirectory = null)
        {
            using (var cofd = new CommonOpenFileDialog()
            {
                Title = "フォルダを選択してください",
                InitialDirectory = initialDirectory ?? @"C:\Users\Public",
                // フォルダ選択モードにする
                IsFolderPicker = isFolderPicker,
            })
            {
                if (extenstion != null)
                {
                    cofd.DefaultExtension = extenstion;
                    cofd.DefaultFileName = extenstion;
                }
                if (cofd.ShowDialog() != CommonFileDialogResult.Ok)
                {
                    return null;
                }

                return cofd.FileName;
            }
        }

        private IEnumerable<string> GetContentLines(Format format = Format.Tsv)
        {
            if (SelectedOutputType.Value.OutputType == OutputType.StringOnly)
            {
                return LineInfoList.Select(x => x.Text);
            }
            else if (SelectedOutputType.Value.OutputType == OutputType.FileNameOnly)
            {
                return LineInfoList.Select(x => x.FullPath).Distinct();
            }
            else if (SelectedOutputType.Value.OutputType == OutputType.DetailData)
            {
                return LineInfoList.Select(x => format.LineCreate(x));
            }
            throw new ArgumentException();
        }

        private void ReadExcel(string fileName, Action<DataTable> action)
        {
            try
            {
                using (FileStream stream = File.Open(fileName, FileMode.Open, FileAccess.Read))
                {
                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        //全シート全セルを読み取り
                        var dataset = reader.AsDataSet();
                        for (var i = 0; i < dataset.Tables.Count; i++)
                        {
                            var worksheet = dataset.Tables[i];

                            if (worksheet is null)
                            {
                                continue;
                            }

                            action.Invoke(worksheet);
                        }
                    }
                }
            }
            catch (Exception)
            {

            }
        }
        private Task<List<LineInfo>> SearchFile(string fileName, string text, CancellationToken token)
        {
            Func<string, string, List<LineInfo>> action = (fileName, text) =>
            {
                var list = new List<LineInfo>();

                var fName = Path.GetFileName(fileName);
                if (fName.Contains("~$") && (fileName.EndsWith(".xls") || fileName.EndsWith(".xlsx") || fileName.EndsWith(".xlsb")))
                {
                    return list;
                }

                if (fileName.EndsWith(".xls") || fileName.EndsWith(".xlsx") || fileName.EndsWith(".xlsb"))
                {
                    Action<DataTable> action = (worksheet) =>
                    {
                        //セルの入力文字を読み取り
                        for (var row = 0; row < worksheet.Rows.Count; row++)
                        {
                            for (var col = 0; col < worksheet.Columns.Count; col++)
                            {
                                var cell = worksheet.Rows[row][col];
                                var line = cell?.ToString() ?? "";
                                if (line != null)
                                {
                                    var result = MatchText(line, text, UseRegex.Value, !CaseSensitive.Value);
                                    foreach (var r in result)
                                    {
                                        var info = new LineInfo();
                                        info.FilePath = Path.GetDirectoryName(fileName) ?? string.Empty;
                                        info.FileName = Path.GetFileName(fileName);
                                        info.Line = (row + 1).ToString();
                                        info.Sheet = worksheet.TableName;
                                        info.Text = r;
                                        list.Add(info);
                                    }
                                }
                            }
                        }
                    };

                    ReadExcel(fileName, action);
                }
                else
                {
                    DetectionResult charsetDetectedResult;

                    try
                    {
                        using (var stream = new FileStream(fileName, FileMode.Open))
                        {
                            charsetDetectedResult = DetectFromStream(stream);
                        }

                        if (charsetDetectedResult.Detected == null)
                        {
                            return list;
                        }

                        // ファイルをオープンする
                        using (var sr = new StreamReader(fileName, charsetDetectedResult.Detected.Encoding))
                        {
                            int lineNo = 1;
                            while (0 <= sr.Peek())
                            {
                                // キャンセルトークンの状態を監視する
                                if (token.IsCancellationRequested)
                                {
                                    // キャンセルされた場合は、OperationCanceledExceptionをスローする
                                    throw new OperationCanceledException(token);
                                }

                                var line = sr.ReadLine();

                                if (line != null)
                                {
                                    var result = MatchText(line, text, UseRegex.Value, !CaseSensitive.Value);
                                    foreach (var r in result)
                                    {
                                        var info = new LineInfo();
                                        info.FilePath = Path.GetDirectoryName(fileName) ?? string.Empty;
                                        info.FileName = Path.GetFileName(fileName);
                                        info.Line = lineNo.ToString();
                                        info.Sheet = string.Empty;
                                        info.Text = r;
                                        list.Add(info);
                                    }
                                }

                                lineNo++;
                            }
                        }
                    }
                    catch (OperationCanceledException ex)
                    {
                        throw ex;
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex);
                    }
                    finally
                    {

                    }
                }

                return list;
            };

            // タスクを開始して、キャンセルトークンを渡す
            return Task.Run(() => action(fileName, text), token);
        }

        private Task ReadFile(string fileName, Action<string?> action)
        {
            return Task.Run(() =>
            {
                DetectionResult charsetDetectedResult;

                using (var stream = new FileStream(fileName, FileMode.Open))
                {
                    charsetDetectedResult = DetectFromStream(stream);
                }

                // ファイルをオープンする
                using (var sr = new StreamReader(fileName, charsetDetectedResult.Detected.Encoding))
                {
                    while (0 <= sr.Peek())
                    {
                        var line = sr.ReadLine();

                        action(line);
                    }
                }
            });
        }

        private List<string> MatchText(string line, string text, bool useRegex, bool ignoreCase)
        {
            var result = new List<string>();
            if (useRegex)
            {
                var regexOption = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
                // Regexオブジェクトを作成
                var regex = new Regex(text, regexOption);
                // 最初の一致する文字列を検索
                var match = regex.Match(line);

                // すべての一致する文字列を出力
                while (match.Success)
                {
                    result.Add(line);
                    if (CombineMatches.Value)
                    {
                        return result;
                    }
                    match = match.NextMatch();
                }
            }
            else
            {
                var stringComparison = ignoreCase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.Ordinal;

                int index = line.IndexOf(text, stringComparison);

                while (index != -1)
                {
                    result.Add(line);
                    if (CombineMatches.Value)
                    {
                        return result;
                    }
                    index = line.IndexOf(text, index + 1, StringComparison.OrdinalIgnoreCase);
                }
            }

            return result;
        }

        private void LoadHistory()
        {
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Process.GetCurrentProcess().ProcessName, "SearchHistory.xml");
            if (File.Exists(filePath))
            {
                var history = XmlHelper.Deserialize<SearchHistory>(filePath);
                SearchHistory.ClearAndAddAllSafe(history.SearchTextHistory);
                SearchDirectoryHistory.ClearAndAddAllSafe(history.SearchDirectoryHistory);
                SearchFileNameHistory.ClearAndAddAllSafe(history.SearchFileNameHistory);
            }
        }

        public void SaveHistory()
        {
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Process.GetCurrentProcess().ProcessName, "SearchHistory.xml");
            var history = new SearchHistory();
            history.SearchTextHistory = new List<string>(SearchHistory.ToList());
            history.SearchDirectoryHistory = new List<string>(SearchDirectoryHistory.ToList());
            history.SearchFileNameHistory = new List<string>(SearchFileNameHistory.ToList());
            XmlHelper.Serialize(history, filePath);
        }
        private void UpdateElapsed()
        {
            var totalFiles = Maximum.Value;
            var filesSearched = Counter.Value;
            var elapsed = _Stopwatch.Elapsed;

            try
            {
                SearchingInfoPercent.Value = $"{100 * filesSearched / totalFiles}%";
                SearchingInfo.Value = $"検索ファイル数 {filesSearched}/{totalFiles} 経過時間 {elapsed:hh\\:mm\\:ss}";
                var remaining = TimeSpan.FromSeconds((totalFiles - filesSearched) * elapsed.TotalSeconds / filesSearched);
                SearchingInfo.Value = $"{SearchingInfo.Value} 残り時間 {remaining:hh\\:mm\\:ss}";
            }
            catch (OverflowException)
            {

            }
        }

        private DetectionResult DetectFromStream(Stream stream)
        {
            return CharsetDetector.DetectFromStream(stream, 1024 * 1024);
        }
    }
}
