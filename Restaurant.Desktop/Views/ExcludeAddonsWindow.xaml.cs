using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Restaurant.Models.DTOs;

namespace Restaurant.Desktop.Views
{
    public class ExcludableAddonOption
    {
        public string Name { get; set; } = string.Empty;
        public bool IsExcluded { get; set; }
    }

    public partial class ExcludeAddonsWindow : Window
    {
        public ObservableCollection<ExcludableAddonOption> AddonOptions { get; set; } = new();
        public string ResultNotes { get; private set; } = "لا يوجد ملاحظات";

        public ExcludeAddonsWindow(List<AddonDto> allAddons, string currentNotes)
        {
            InitializeComponent();
            DataContext = this;

            // Parse current notes to check which addons are already excluded
            // Notes are formatted as: "بدون بصل، بدون جبنة"
            var excludedList = new List<string>();
            if (!string.IsNullOrWhiteSpace(currentNotes) && currentNotes != "لا يوجد ملاحظات")
            {
                excludedList = currentNotes
                    .Split(new[] { '،', ',' }, System.StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Replace("بدون", "").Trim())
                    .ToList();
            }

            foreach (var addon in allAddons)
            {
                bool isExcluded = excludedList.Any(ex => ex.Equals(addon.Name, System.StringComparison.OrdinalIgnoreCase));
                AddonOptions.Add(new ExcludableAddonOption
                {
                    Name = addon.Name,
                    IsExcluded = isExcluded
                });
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var excludedNames = AddonOptions
                .Where(o => o.IsExcluded)
                .Select(o => o.Name)
                .ToList();

            if (excludedNames.Any())
            {
                ResultNotes = "بدون " + string.Join("، بدون ", excludedNames);
            }
            else
            {
                ResultNotes = "لا يوجد ملاحظات";
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
