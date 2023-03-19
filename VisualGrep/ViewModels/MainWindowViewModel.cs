using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using UtfUnknown;
using VisualGrep.Models;
using VisualGrep.Utls;


namespace VisualGrep.ViewModels
{
    public class MainWindowViewModel
    {
        public ReactiveProperty<string> FolderPath { get; } = new ReactiveProperty<string>(string.Empty);
        public ReactiveProperty<string> SearchText { get; } = new ReactiveProperty<string>(string.Empty);

        public ReactiveProperty<bool> SearchButtonEnable { get; } = new ReactiveProperty<bool>(true);

        public ReactiveCommand SearchCommand { get; } = new ReactiveCommand();
        public ObservableCollection<LineInfo> LineInfoList { get; } = new ObservableCollection<LineInfo>();



        public MainWindowViewModel()
        {
            BindingOperations.EnableCollectionSynchronization(LineInfoList, new object());
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            SearchCommand.Subscribe(async e =>
            {
                SearchButtonEnable.Value = false;
                LineInfoList.Clear();

                if (FolderPath.Value == string.Empty)
                {
                    return;
                }

                if (!Directory.Exists(FolderPath.Value))
                {
                    return;
                }

                var files = FileUtils.GetAllFiles(FolderPath.Value).Select((value, index) => value)
                .ToList();

                await Task.Run(async () =>
                {
                    //同期処理でリスト化しているのでここでUIスレッドが固まらなくなる
                    foreach (var file in files)
                    {
                        //10ファイルパスだとすぐに終わってしまう為少し待たせる
                        var list = await SearchFile(file, SearchText.Value);
                        foreach(var info in list)
                        {
                            LineInfoList.Add(info);
                        }
                    }
                });

                SearchButtonEnable.Value = true;
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

                        if (line != null && line.Contains(text))
                        {
                            var info = new LineInfo();
                            info.FilePath = Path.GetDirectoryName(fileName);
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
    }
}
