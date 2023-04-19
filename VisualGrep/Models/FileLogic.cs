using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using VisualGrep.Utls;

namespace VisualGrep.Models
{
    public class FileLogic
    {
        public static string? SelectPath(bool isFolderPicker = false, string? extenstion = null, string? initialDirectory = null)
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
        public static IEnumerable<string> GetContentLines(ObservableCollection<LineInfo> lineInfoList, OutputType outputType, Format format = Format.Tsv)
        {
            if (outputType == OutputType.StringOnly)
            {
                return lineInfoList.Select(x => x.Text);
            }
            else if (outputType == OutputType.FileNameOnly)
            {
                return lineInfoList.Select(x => x.FullPath).Distinct();
            }
            else if (outputType == OutputType.DetailData)
            {
                return lineInfoList.Select(x => format.LineCreate(x));
            }
            throw new ArgumentException();
        }
        public static bool CheckFileName(string fileName, Regex? regex, Regex? excludeRegex)
        {
            if (excludeRegex is not null)
            {
                if (excludeRegex.Match(fileName).Success)
                {
                    return false;
                }
            }

            if (regex is null)
            {
                return true;
            }

            return regex.Match(fileName).Success;
        }
    }
}
