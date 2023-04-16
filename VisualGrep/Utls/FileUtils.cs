using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VisualGrep.Models;

namespace VisualGrep.Utls
{
    public static class FileUtils
    {
        public static string TsvLineCreate(params object[] args)
        {
            return string.Join("\t", args);
        }
        public static string CsvLineCreate(params object[] args)
        {
            return string.Join(",", args);
        }

        private static readonly HashSet<string> _exceptFolder = new HashSet<string>
        {
            @"C:\Windows", //システムファイルが多すぎる
            @"C:\Users\All Users",
            @"C:\$Recycle.Bin", //ゴミ箱
            @"C:\Recovery",
            @"C:\Config.Msi", //起動して最初に実行されるらしい
            @"C:\Documents and Settings", //デスクトップとかマイドキュメントなど
            @"C:\System Volume Information",
            @"C:\Program Files\windows nt\アクセサリ",
            @"C:\ProgramData\Application Data", //よくある隠しフォルダ
        };
        public static IEnumerable<string> GetAllFiles(string folderPath, bool includeSubfolders = true)
        {
            return includeSubfolders == false ? Directory.EnumerateFiles(folderPath) : GetAllFilesEnumerate(folderPath);
        }

        public static IEnumerable<string> GetAllFilesEnumerate(string folderPath)
        {
            var directories = Enumerable.Empty<string>();

            try
            {
                directories = Directory.EnumerateDirectories(folderPath)
                    .Where(x => _exceptFolder.All(y => !x.StartsWith(y, StringComparison.CurrentCultureIgnoreCase)))
                    .SelectMany(GetAllFilesEnumerate);
            }
            catch
            {
                return directories;
            }

            //同階層のファイル取得をして再帰的に同階層のフォルダを検索しに行く
            return Directory.EnumerateFiles(folderPath).Concat(directories);
        }

        public static FileType GetFileType(string filePath)
        {
            var ext = Path.GetExtension(filePath);
            if (ext == ".txt" || ext == ".csv" || ext == ".tsv" || ext == ".log" || ext == ".xml" || ext == ".json" || ext == ".html" || ext == ".md")
            {
                return FileType.Text;
            }
            else if (ext == ".xls" || ext == ".xlsx" || ext == ".xlsb" || ext == ".xlsm")
            {
                return FileType.Excel;
            }

            return FileType.Others;
        }
    }
}
