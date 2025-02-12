using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;

namespace ImageProcessor
{
    public class LanguageLoading
    {
        private const string LanguageFilePath = "TesseractLanguages.txt";
        private const string LastUsedFilePath = "LastUsedLanguage.txt";

        public void LoadLanguages(ComboBox fromComboBox, ComboBox toComboBox)
        {
            if (!File.Exists(LanguageFilePath))
            {
                MessageBox.Show("Language file not found.");
                return;
            }

            var allLanguages = File.ReadAllLines(LanguageFilePath)
                .Select(line =>
                {
                    var parts = line.Split(new[] { "Content=\"", "\" Tag=\"", "\" GoogleTag=\"" }, StringSplitOptions.None);
                    if (parts.Length < 4) return null;

                    string content = parts[1] ?? string.Empty;
                    string tesseractTag = parts[2]?.TrimEnd('"') ?? string.Empty;
                    string googleTag = parts[3]?.TrimEnd('"') ?? string.Empty;
                    return new ComboBoxItem { Content = content, Tag = new List<string> { tesseractTag, googleTag } };
                })
                .Where(item => item != null)
                .OrderBy(item => item!.Content.ToString()) // Sort A-Z
                .ToList();

            var lastUsedFrom = LoadLastUsedLanguages(fromComboBox.Name);
            var lastUsedTo = LoadLastUsedLanguages(toComboBox.Name);

            fromComboBox.Items.Clear();
            toComboBox.Items.Clear();

            if (lastUsedFrom.Count > 0)
            {
                fromComboBox.Items.Add(new ComboBoxItem { Content = "---- Last Used ----", IsEnabled = false });
                foreach (var item in lastUsedFrom)
                {
                    fromComboBox.Items.Add(item);
                }
            }
            
            if (lastUsedTo.Count > 0)
            {
                toComboBox.Items.Add(new ComboBoxItem { Content = "---- Last Used ----", IsEnabled = false });
                foreach (var item in lastUsedTo)
                {
                    toComboBox.Items.Add(item);
                }
            }

            fromComboBox.Items.Add(new ComboBoxItem { Content = "---- A-Z ----", IsEnabled = false });
            toComboBox.Items.Add(new ComboBoxItem { Content = "---- A-Z ----", IsEnabled = false });

            foreach (var item in allLanguages)
            {
                if (item != null)
                {
                    fromComboBox.Items.Add(item);
                    toComboBox.Items.Add(item.Clone()!);
                }
            }

            if (lastUsedFrom.Count > 0)
                fromComboBox.SelectedItem = lastUsedFrom[0];
            if (lastUsedTo.Count > 0)
                toComboBox.SelectedItem = lastUsedTo[0];
        }


        private List<ComboBoxItem> LoadLastUsedLanguages(string comboBoxName)
        {
            var lastUsedLanguages = new List<ComboBoxItem>();

            if (!File.Exists(LastUsedFilePath))
                return lastUsedLanguages;

            var lastUsedLines = File.ReadAllLines(LastUsedFilePath)
                                    .Where(line => line.StartsWith(comboBoxName))
                                    .Select(line => line.Substring(comboBoxName.Length + 1))
                                    .ToList();

            foreach (var line in lastUsedLines)
            {
                var parts = line.Split(new[] { "Content=\"", "\" Tag=\"", "\" GoogleTag=\"" }, StringSplitOptions.None);
                if (parts.Length < 4) continue;

                string content = parts[1] ?? string.Empty;
                string tesseractTag = parts[2]?.TrimEnd('"') ?? string.Empty;
                string googleTag = parts[3]?.TrimEnd('"') ?? string.Empty;

                lastUsedLanguages.Add(new ComboBoxItem { Content = content, Tag = new List<string> { tesseractTag, googleTag } });
            }

            return lastUsedLanguages;
        }

        public void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string content = selectedItem.Content?.ToString() ?? string.Empty;
                var tags = selectedItem.Tag as List<string> ?? new List<string> { "", "" };
                SaveLastUsedLanguage(comboBox.Name, content, tags[0], tags[1]);
            }
        }

        private void SaveLastUsedLanguage(string comboBoxName, string content, string tesseractTag, string googleTag)
        {
            var lastUsed = new Dictionary<string, List<string>>();

            if (File.Exists(LastUsedFilePath))
            {
                foreach (var line in File.ReadAllLines(LastUsedFilePath))
                {
                    var parts = line.Split(new[] { " " }, 2, StringSplitOptions.None);
                    if (parts.Length < 2) continue;

                    string key = parts[0];
                    string value = parts[1];

                    if (!lastUsed.ContainsKey(key))
                        lastUsed[key] = new List<string>();

                    lastUsed[key].Add(value);
                }
            }

            if (!lastUsed.ContainsKey(comboBoxName))
                lastUsed[comboBoxName] = new List<string>();

            lastUsed[comboBoxName].RemoveAll(line => line.Contains($"Content=\"{content}\" Tag=\"{tesseractTag}\" GoogleTag=\"{googleTag}\""));
            lastUsed[comboBoxName].Insert(0, $"Content=\"{content}\" Tag=\"{tesseractTag}\" GoogleTag=\"{googleTag}\""
            );

            if (lastUsed[comboBoxName].Count > 3)
                lastUsed[comboBoxName] = lastUsed[comboBoxName].Take(3).ToList();

            var linesToWrite = lastUsed.SelectMany(kvp => kvp.Value.Select(v => $"{kvp.Key} {v}")).ToList();
            File.WriteAllLines(LastUsedFilePath, linesToWrite);
        }
    }

    public static class ComboBoxExtensions
    {
        public static ComboBoxItem? Clone(this ComboBoxItem item)
        {
            return item == null ? null : new ComboBoxItem { Content = item.Content, Tag = new List<string>(item.Tag as List<string> ?? new List<string> { "", "" }) };
        }
    }
}
