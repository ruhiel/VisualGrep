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
        public ReactiveCommand<MouseButtonEventArgs> LineInfoMouseDoubleClickCommand { get; } = new ReactiveCommand<MouseButtonEventArgs>();
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
        public MainWindowViewModel()
        {
            BindingOperations.EnableCollectionSynchronization(LineInfoList, new object());
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            
            SearchEnable = FolderPath.CombineLatest(SearchingFlag, (path, y) => !string.IsNullOrEmpty(path) && !y).ToReactiveProperty();
            
            SearchTextWatermark = UseRegex.Select(x => x ? "正規表現" : "検索文字列").ToReadOnlyReactiveProperty();

            ControlEnable = SearchingFlag.Select(x => !x).ToReadOnlyReactiveProperty();

            SearchStopEnable = SearchingFlag.Select(x => x).ToReadOnlyReactiveProperty();

            ExcelPanelVisibility = TextPanelVisibility.Select(x => x == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible).ToReactiveProperty();

            OutputTypeViewModel? first = null;

            foreach (var outputType in Enum.GetValues(typeof(OutputType)))
            {
                var vm = new OutputTypeViewModel((OutputType)outputType);
                if(first is null)
                {
                    first = vm;
                }
                OutputTypeList.Add(vm);
            }

            SelectedOutputType.Value = first;

            SearchCommand.Subscribe(async e =>
            {
                // Stopwatchオブジェクトを作成
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                LineInfoList.Clear();

                if (FolderPath.Value == string.Empty)
                {
                    return;
                }

                if (!Directory.Exists(FolderPath.Value))
                {
                    return;
                }

                var files = FileUtils.GetAllFiles(FolderPath.Value, IncludeSubfolders.Value)
                    .Select((value, index) => value)
                    .Where(x => SearchFileName.Value == string.Empty ? true : x.Contains(SearchFileName.Value))
                    .ToList();

                try
                {
                    await Task.Run(async () =>
                    {
                        //同期処理でリスト化しているのでここでUIスレッドが固まらなくなる
                        foreach (var file in files)
                        {
                            SearchFilePath.Value = file;
                            var list = await SearchFile(file, SearchText.Value, _CancellationTokenSource.Token);
                            foreach (var info in list)
                            {
                                LineInfoList.Add(info);
                            }
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
                    LineInfoList.Clear();
                    foreach(var info in tempList)
                    {
                        LineInfoList.Add(info);
                    }

                    LineInfoListOutputEnabled.Value = LineInfoList.Any();

                    stopwatch.Stop();

                    SearchFilePath.Value = $"終了しました。({stopwatch.Elapsed:mm\\:ss\\.fff})";
                }
            });

            StopCommand.Subscribe(e =>
            {
                _CancellationTokenSource.Cancel();
            });

            DropCommand.Subscribe(e =>
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if(files.Any())
                {
                    var path = files.First();
                    var dir = Directory.Exists(path) ? path : Path.GetDirectoryName(path);

                    if(dir != null)
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

            LineInfoMouseDoubleClickCommand.Subscribe(e =>
            {
                var proc = new System.Diagnostics.Process();

                if(SelectedLineInfo.Value == null) return;

                proc.StartInfo.FileName = SelectedLineInfo.Value.FullPath;
                proc.StartInfo.UseShellExecute = true;
                proc.Start();
            });

            LineInfoSelectionChanged.Subscribe(e =>
            {
                var list = new List<RichTextItem>();

                var grid = e.Source as DataGrid;
                var info = grid?.SelectedItem as LineInfo;

                if(info == null)
                {
                    return;
                }
                
                var ext = Path.GetExtension(info.FullPath);

                if (ext == ".txt")
                {
                    TextPanelVisibility.Value = Visibility.Visible;
                    if (info != null)
                    {
                        ReadFile(info.FullPath, (line) =>
                        {
                            var text = new RichTextItem();
                            text.Text = line ?? string.Empty;
                            text.Foreground = Brushes.Black;

                            list.Add(text);
                        });

                        OutMessage.Value = list;
                    }
                }
                else if(ext == ".xls" || ext == ".xlsx" || ext == ".xlsb")
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
                            text.Foreground = Brushes.Black;
                            var line = string.Empty;
                            
                            for (var col = 0; col < worksheet.Columns.Count; col++)
                            {
                                var cell = worksheet.Rows[row][col];
                                line += cell?.ToString() ?? "";
                            }

                            text.Text = line;

                            list.Add(text);
                        }

                        panel.OutMessage.Value = list;

                        TabPanels.Add(panel);
                    };

                    ReadExcel(info.FullPath, action);
                }

            });

            LineInfoListOutputCommand.Subscribe(e =>
            {
                var name = Path.GetTempFileName();
                using (StreamWriter sw = new StreamWriter(name, false, Encoding.UTF8))
                {
                    foreach(var line in GetContentLines())
                    {
                        sw.WriteLine(line);
                    }
                }

                var proc = new System.Diagnostics.Process();

                proc.StartInfo.FileName = name;
                proc.StartInfo.UseShellExecute = true;
                proc.Start();
            });
        }
        private IEnumerable<string> GetContentLines()
        {
            if(SelectedOutputType.Value.OutputType == OutputType.StringOnly)
            {
                return LineInfoList.Select(x => x.Text);
            }
            else if (SelectedOutputType.Value.OutputType == OutputType.FileNameOnly)
            {
                return LineInfoList.Select(x => x.FullPath).Distinct();
            }
            else if (SelectedOutputType.Value.OutputType == OutputType.DetailData)
            {
                return LineInfoList.Select(x => FileUtils.TsvLineCreate(x.FullPath, x.Line.ToString(), x.Text));
            }
            throw new ArgumentException();
        }

        private void ReadExcel(string fileName, Action<DataTable> action)
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
        private Task<List<LineInfo>> SearchFile(string fileName, string text, CancellationToken token)
        {
            Func<string, string, List<LineInfo>> action = (fileName, text) =>
            {
                var list = new List<LineInfo>();

                var fName = Path.GetFileName(fileName);
                if(fName.Contains("~$") && (fileName.EndsWith(".xls") || fileName.EndsWith(".xlsx") || fileName.EndsWith(".xlsb")))
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

                    using (var stream = new FileStream(fileName, FileMode.Open))
                    {
                        charsetDetectedResult = CharsetDetector.DetectFromStream(stream);
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

                            if (line != null )
                            {
                                var result = MatchText(line, text, UseRegex.Value, !CaseSensitive.Value);
                                foreach(var r in result)
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



                return list;
            };

            // タスクを開始して、キャンセルトークンを渡す
            return Task.Run(() => action(fileName, text), token);
        }

        private void ReadFile(string fileName, Action<string?> action)
        {
            DetectionResult charsetDetectedResult;

            using (var stream = new FileStream(fileName, FileMode.Open))
            {
                charsetDetectedResult = CharsetDetector.DetectFromStream(stream);
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
        }

        private List<string> MatchText(string line, string text, bool useRegex, bool ignoreCase)
        {
            var result = new List<string>();
            if(useRegex)
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
                    if(CombineMatches.Value)
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
    }
}
