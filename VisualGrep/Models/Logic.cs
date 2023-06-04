using ExcelDataReader;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UtfUnknown;
using VisualGrep.Utls;

namespace VisualGrep.Models
{
    public class Logic
    {
        static private Logger logger = LogManager.GetCurrentClassLogger();
        public static List<string> MatchText(string line, string text, bool useRegex, bool ignoreCase, bool combineMatches)
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
                    if (combineMatches)
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
                    if (combineMatches)
                    {
                        return result;
                    }
                    index = line.IndexOf(text, index + 1, StringComparison.OrdinalIgnoreCase);
                }
            }

            return result;
        }



        public static void LoadHistory(ObservableCollection<string> searchHistory, ObservableCollection<string> searchDirectoryHistory, ObservableCollection<string> searchFileNameHistory, ObservableCollection<string> excludeFilePathHistory)
        {
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Process.GetCurrentProcess().ProcessName, "SearchHistory.xml");
            if (File.Exists(filePath))
            {
                var history = XmlHelper.Deserialize<SearchHistory>(filePath);
                searchHistory.ClearAndAddAllSafe(history.SearchTextHistory);
                searchDirectoryHistory.ClearAndAddAllSafe(history.SearchDirectoryHistory);
                searchFileNameHistory.ClearAndAddAllSafe(history.SearchFileNameHistory);
                excludeFilePathHistory.ClearAndAddAllSafe(history.ExcludeFilePathHistory);
            }
        }

        public static void SaveHistory(ObservableCollection<string> searchHistory, ObservableCollection<string> searchDirectoryHistory, ObservableCollection<string> searchFileNameHistory, ObservableCollection<string> excludeFilePathHistory)
        {
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Process.GetCurrentProcess().ProcessName, "SearchHistory.xml");
            var history = new SearchHistory();
            history.SearchTextHistory = new List<string>(searchHistory.ToList());
            history.SearchDirectoryHistory = new List<string>(searchDirectoryHistory.ToList());
            history.SearchFileNameHistory = new List<string>(searchFileNameHistory.ToList());
            history.ExcludeFilePathHistory = new List<string>(excludeFilePathHistory.ToList());
            XmlHelper.Serialize(history, filePath);
        }


        public static DetectionResult DetectFromStream(Stream stream)
        {
            return CharsetDetector.DetectFromStream(stream, 1024 * 1024);
        }

        public static Task ReadFile(string fileName, Action<string?> action)
        {
            return Task.Run(() =>
            {
                DetectionResult charsetDetectedResult;

                using (var stream = new FileStream(fileName, FileMode.Open))
                {
                    charsetDetectedResult = Logic.DetectFromStream(stream);
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

        public static Task<List<LineInfo>> SearchFile(string fileName, string text, CancellationToken token, bool useRegex, bool caseSensitive, bool combineMatches)
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
                                    var result = Logic.MatchText(line, text, useRegex, !caseSensitive, combineMatches);
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
                            charsetDetectedResult = Logic.DetectFromStream(stream);
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
                                    var result = Logic.MatchText(line, text, useRegex, !caseSensitive, combineMatches);
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

        public static void ReadExcel(string fileName, Action<DataTable> action)
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
    }
}
