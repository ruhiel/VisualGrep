﻿using Reactive.Bindings;
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


namespace VisualGrep.ViewModels
{
    public class MainWindowViewModel
    {
        public ReactiveProperty<string> FolderPath { get; } = new ReactiveProperty<string>(string.Empty);
        public ReactiveProperty<string> SearchText { get; } = new ReactiveProperty<string>(string.Empty);
        public ReactiveProperty<string> SearchFileName { get; } = new ReactiveProperty<string>(string.Empty);
        public ReactiveProperty<bool> SearchEnable { get; } = new ReactiveProperty<bool>(true);
        public ReactiveProperty<bool> IncludeSubfolders { get; } = new ReactiveProperty<bool>(true);
        public ReactiveProperty<bool> UseRegex { get; } = new ReactiveProperty<bool>(false);
        public ReactiveProperty<bool> CaseSensitive { get; } = new ReactiveProperty<bool>(false);
        public ReactiveCommand SearchCommand { get; } = new ReactiveCommand();
        public ReactiveCommand<DragEventArgs> DropCommand { get; } = new ReactiveCommand<DragEventArgs>();
        public ReactiveCommand<DragEventArgs> PreviewDragOverCommand { get; } = new ReactiveCommand<DragEventArgs>();
        public ReactiveProperty<LineInfo> SelectedLineInfo { get; } = new ReactiveProperty<LineInfo>();
        public ReactiveCommand<MouseButtonEventArgs> LineInfoMouseDoubleClickCommand { get; } = new ReactiveCommand<MouseButtonEventArgs>();

        public ObservableCollection<LineInfo> LineInfoList { get; } = new ObservableCollection<LineInfo>();
        public ReactiveProperty<string> SearchFilePath { get; } = new ReactiveProperty<string>(string.Empty);

        public MainWindowViewModel()
        {
            BindingOperations.EnableCollectionSynchronization(LineInfoList, new object());
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            SearchCommand.Subscribe(async e =>
            {
                SearchEnable.Value = false;
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

                await Task.Run(async () =>
                {
                    //同期処理でリスト化しているのでここでUIスレッドが固まらなくなる
                    foreach (var file in files)
                    {
                        //10ファイルパスだとすぐに終わってしまう為少し待たせる
                        SearchFilePath.Value = file;
                        var list = await SearchFile(file, SearchText.Value);
                        foreach(var info in list)
                        {
                            LineInfoList.Add(info);
                        }
                    }
                });

                SearchFilePath.Value = string.Empty;
                SearchEnable.Value = true;
            });

            DropCommand.Subscribe(e =>
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if(files.Any())
                {
                    var dir = Path.GetDirectoryName(files.First());

                    if(dir != null)
                    {
                        FolderPath.Value = dir;
                    }

                    var extension = Path.GetExtension(files.First());

                    if (extension != null)
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

                proc.StartInfo.FileName = SelectedLineInfo.Value.FullPath;
                proc.StartInfo.UseShellExecute = true;
                proc.Start();
            });
        }

        private Task<List<LineInfo>> SearchFile(string fileName, string text)
        {
            Func<string, string, List<LineInfo>> action = (fileName, text) =>
            {
                var list = new List<LineInfo>();

                DetectionResult charsetDetectedResult;

                using (var stream = new FileStream(fileName, FileMode.Open))
                {
                    charsetDetectedResult = CharsetDetector.DetectFromStream(stream);
                }

                if(charsetDetectedResult.Detected == null)
                {
                    return list;
                }

                // ファイルをオープンする
                using (var sr = new StreamReader(fileName, charsetDetectedResult.Detected.Encoding))
                {
                    int lineNo = 1;
                    while (0 <= sr.Peek())
                    {
                        var line = sr.ReadLine();

                        if (line != null && MatchText(line, text, UseRegex.Value, !CaseSensitive.Value))
                        {
                            var info = new LineInfo();
                            info.FilePath = Path.GetDirectoryName(fileName) ?? string.Empty;
                            info.FileName = Path.GetFileName(fileName);
                            info.Line = lineNo.ToString();
                            info.Text = line;
                            list.Add(info);
                        }

                        lineNo++;
                    }
                }

                return list;
            };

            return Task.Factory.StartNew(() => action(fileName, text));
        }

        private bool MatchText(string? line, string text, bool useRegex, bool ignoreCase)
        {
            if(useRegex)
            {
                var regexOption = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;

                return !string.IsNullOrEmpty(line) && Regex.IsMatch(line, text, regexOption);
            }
            else
            {
                var stringComparison = ignoreCase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.Ordinal;

                return !string.IsNullOrEmpty(line) && line.IndexOf(text, stringComparison) >= 0;
            }
        }
    }
}
